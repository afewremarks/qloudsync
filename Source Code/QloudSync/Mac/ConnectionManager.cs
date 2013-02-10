



using Amazon.S3;
using Amazon.S3.Model;

using System;
using System.Net;
using System.Configuration;
using System.IO;
using System.Collections.Specialized;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using GreenQloud.Util;
using GreenQloud.Repository;
using System.Text;


namespace GreenQloud.Net.S3
{
	public class ConnectionManager
	{
        string bucketName;
		public ConnectionManager (string bucketName)
		{
            this.bucketName = bucketName;
		}

        public int TransferSize {
            set; get;
        }

		#region Connection

        private AmazonS3Client connection;       

        public static void Authenticate (string username, string password)
        {
            Uri uri = new Uri(GlobalSettings.AuthenticationURL);
            
            WebRequest myReq = WebRequest.Create (uri);
            string usernamePassword = username + ":" + password;
            CredentialCache mycache = new CredentialCache ();
            mycache.Add (uri, "Basic", new NetworkCredential (username, password));
            myReq.Credentials = mycache;
            myReq.Headers.Add ("Authorization", "Basic " + Convert.ToBase64String (new ASCIIEncoding ().GetBytes (usernamePassword)));
            WebResponse wr = myReq.GetResponse ();
            Stream receiveStream = wr.GetResponseStream ();
            StreamReader reader = new StreamReader (receiveStream, Encoding.UTF8);
            string receiveContent = reader.ReadToEnd ();
//FIXME The base authentication response is JSON format. Doing a substring using an index will brake. Fix by using a JSON parser. 
            Credential.SecretKey = receiveContent.Substring(Constant.KEY_SECRET_START_INDEX, Constant.KEYS_LENGTH);
            Credential.PublicKey = receiveContent.Substring(Constant.KEY_PUBLIC_START_INDEX, Constant.KEYS_LENGTH);
            
        }   
        
        public AmazonS3Client Connect ()
        {
            if (connection != null)
                return connection;
            
            try {    
                AmazonS3Config config = CreateConfig ();
                connection = (AmazonS3Client)Amazon.AWSClientFactory.CreateAmazonS3Client (Credential.PublicKey, Credential.SecretKey, config);
                return connection;
            } catch (System.Net.WebException) {
                Logger.LogInfo ("Connection", "Failed to communicate with the remote server");
            } catch (Exception e){
                Logger.LogInfo ("Connection", e);
            }
            return null;
        }

        public void Reset ()
        {
            connection = null;
        }

		public AmazonS3Client Reconnect ()
		{
			if (Credential.PublicKey == null || Credential.SecretKey == null) {
				if (Credential.Username == null || Credential.Password == null){
					return null;
				}
				Authenticate (Credential.Username, Credential.Password);
			}			
			return Connect();
		}
        
        private static AmazonS3Config CreateConfig()
        {
            AmazonS3Config conf = new AmazonS3Config();
            conf.ServiceURL =  new Uri(GlobalSettings.StorageURL).Host;
            return conf;
        }

		#endregion


		public bool ExistsBucket 
        {
			get {
                    return Reconnect().ListBuckets () .Buckets.Any (b => b.BucketName == bucketName);
       			}
		}

        public bool CreateBucket ()
        {   
            try {
                PutBucketRequest request = new PutBucketRequest ();
                request.BucketName = bucketName;
                Connect ().PutBucket (request);
                Logger.LogInfo("Connection", "Bucket "+bucketName+" was created.");
                return true;
            } catch (Exception e) {
                Logger.LogInfo ("Connection",e);
                return false;
            }
        }

        public List<S3Object> GetFiles ()
        {
            return Reconnect().ListObjects (
                new ListObjectsRequest ().WithBucketName (bucketName)
                ).S3Objects;
        }

		public void Download (GreenQloud.Repository.File  file)
        {
            try {
                string sourcekey = file.AbsolutePath;
                Logger.LogInfo ("Connection", "Download the file " + sourcekey + ".");
			
                GetObjectRequest objectRequest = new GetObjectRequest (){
                    BucketName = bucketName,
    				Key = sourcekey
			    };
               
                using (GetObjectResponse response = Connect ().GetObject(objectRequest)) {
                    using (Stream responseStream = response.ResponseStream) {
                        using (var memoryStream = new MemoryStream())
                        {
                            var data = new byte[16*1024];
                            int bytesRead;
                            do
                            {
                                bytesRead = responseStream.Read(data, 0, data.Length);
                                memoryStream.Write(data, 0, bytesRead);
                                TransferSize += bytesRead;

                            } while (bytesRead > 0);
                            memoryStream.WriteTo (new FileStream (file.FullLocalName,FileMode.Create, FileAccess.ReadWrite));
                            memoryStream.Flush();
                        }
                    } 
                }

            }catch (AmazonS3Exception) {
                if(InitializeBucket())
                    Download (file);
                else
                    Logger.LogInfo ("Connection", "There is a problem of comunication and the file will be sent back.");
            }
            catch (Exception e) {
                Logger.LogInfo ("Error", e);
            }			
		}	

		public bool CreateFolder (GreenQloud.Repository.Folder folder)
		{
			return CreateFolder (folder.Name, folder.RelativePathInBucket);
		}

		public bool CreateTrashFolder ()
		{
			return CreateFolder (Constant.TRASH, bucketName);
		}

		private bool CreateFolder (string name, string relativePath)
		{
			try {
	
				PutObjectRequest putObject = new PutObjectRequest (){
					BucketName = relativePath,
					Key = name,
					ContentBody = string.Empty
				};
				using (S3Response response = Connect ().PutObject (putObject))
				{
					response.Dispose ();
					return true;
				}
                Logger.LogInfo ("Connection","Creating folder "+name+".");
            }catch (AmazonS3Exception) {
                if(InitializeBucket())
                  return  CreateFolder (name, relativePath);
                else{
                    Logger.LogInfo ("Connection", "There is a problem of comunication and the file will be sent back.");
                    return false;
                }
            }
            catch(Exception e) {
				Logger.LogInfo ("Connection","Fail to upload "+e.Message);
				return false;
			}
		}
     
		
		public void GenericDelete (string key)
		{
			try {
				if (key == "")
					return;


				DeleteObjectRequest request = new DeleteObjectRequest (){
				    BucketName = bucketName,
				    Key = key
			    };
                AmazonS3Client connection = Connect();

                using (DeleteObjectResponse response = connection.DeleteObject (request)) {
                    response.Dispose ();
				}
                Logger.LogInfo ("Connection", string.Format("{0} was deleted in bucket.", key));

            } catch (AmazonS3Exception) {
                if(InitializeBucket())
                    GenericDelete (key);
                else
                    Logger.LogInfo ("Connection", "There is a problem of comunication and the file will be sent back.");
            }catch (Exception e){
				Logger.LogInfo ("Connection", e);
			}
		}

		#region Auxiliar

		public TimeSpan CalculateDiffClocks ()
		{
			try {
                GreenQloud.Repository.File clockFile = new RemoteFile (Constant.CLOCK_TIME);
				PutObjectRequest putObject = new PutObjectRequest ()
				{
					BucketName = bucketName,
					Key = clockFile.Name,
					ContentBody = string.Empty
				};
				
				DateTime localClock;
				using (S3Response response = Connect ().PutObject (putObject)){
					localClock = DateTime.Now;
					response.Dispose ();
				}
				
				ListObjectsResponse files = Connect ().ListObjects (new ListObjectsRequest ().WithBucketName (bucketName));
				S3Object remotefile = files.S3Objects.Where (o => o.Key == clockFile.Name).FirstOrDefault();
				string sRemoteclock = remotefile.LastModified;
				
                GenericDelete (clockFile.AbsolutePath);

				DateTime remoteClock = Convert.ToDateTime (sRemoteclock);
				
				return localClock.Subtract(remoteClock);
				
			} catch(Exception e) {
                Logger.LogInfo ("Connection","Fail to determinate a remote clock: "+e.Message +" \n");

                return new TimeSpan (0);
			}
		}


		public bool GetValidationCallBack (Object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate,
		                                   System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors errors)
		{
			return true;

		}


        public void GenericCopy (string sourceBucket, string sourceKey, string destinationBucket, string destinationKey)
        {
            try {
                CopyObjectRequest request = new CopyObjectRequest (){
                    DestinationBucket = destinationBucket,
                    DestinationKey = destinationKey,
                    SourceBucket = sourceBucket,
                    SourceKey = sourceKey
                };

                Connect ().CopyObject (request);
            } 
            //TODO Understand why System.InvalidOperationException is catched, and the file is copied
            catch{
                               
            }
        }

        public void GenericUpload (string bucketName, string key, string filepath)
        {
            try {
                PutObjectRequest putObject = new PutObjectRequest ()
                {
                    BucketName = bucketName,
                    FilePath = filepath,
                    Key = key, 
                    Timeout = GlobalSettings.UploadTimeout
                };
                using (S3Response response = Connect ().PutObject (putObject)) {
                    response.Dispose ();
                }
                Logger.LogInfo ("Connection", string.Format("{0} was uploaded.", filepath));
            } catch (ObjectDisposedException) {
                Logger.LogInfo ("Connection", "An exception occurred, the file will be resend.");
                GenericUpload (bucketName, key, filepath);
            } catch (AmazonS3Exception) {

                if(InitializeBucket())
                    GenericUpload (bucketName, key, filepath);
                else
                    Logger.LogInfo ("Connection", "There is a problem of comunication and the file will be sent back.");
            }
			catch(Exception e) {
                Logger.LogInfo ("Connection",e);
            }
        }

        protected bool InitializeBucket ()
        {
            if (!ExistsBucket) {
                if (CreateBucket ())
                   return CreateTrashFolder ();
            } else {
                if (!GetFiles().Any(s3o => s3o.Key.Contains(GlobalSettings.Trash)))
                    return CreateTrashFolder ();
                return true;
            }
            return false;
        }

		#endregion

		public void DeleteAllFilesInBucket ()
		{
			foreach(S3Object s3 in GetFiles ()){
				DeleteObjectRequest request = new DeleteObjectRequest (){
					BucketName = bucketName,
					Key = s3.Key
				};
				using (DeleteObjectResponse response = Connect ().DeleteObject (request)) {
					response.Dispose ();
				}
                connection = null;
			}
		}
	}
}