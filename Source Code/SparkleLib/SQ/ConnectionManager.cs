

using  SQ.Security;

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
using SQ.Util;
using SQ.Repository;


namespace SQ.Net.S3
{
	public class ConnectionManager
	{
        string bucketName;
		public ConnectionManager (string bucketName)
		{
            this.bucketName = bucketName;
		}

		#region Connection

        private AmazonS3Client connection;       
        
        
        private AmazonS3Client Connect ()
        {
            if(connection != null)
                return connection;
            
            try{    
                AmazonS3Config config = CreateConfig();
                connection = (AmazonS3Client) Amazon.AWSClientFactory.CreateAmazonS3Client (Credential.PublicKey, Credential.SecretKey, config);
                return connection;
            }
            catch (System.Net.WebException)
            {
                Logger.LogInfo("Connection", "Failed to communicate with the remote server");
            }
            return null;
        }

		public AmazonS3Client Reconnect ()
		{
			if (Credential.PublicKey == null || Credential.SecretKey == null) {
				if (Credential.User == null || Credential.Password == null){
					return null;
				}
				new Authentication().Authenticate (Credential.User, Credential.Password);
			}			
			return Connect();
		}
        
        private static AmazonS3Config CreateConfig()
        {
            AmazonS3Config conf = new AmazonS3Config();
            conf.ServiceURL = Credential.URLConnection;
            return conf;
        }

		#endregion


		public bool ExistsBucket {
			get {
				return Reconnect ().ListBuckets () .Buckets.Where (b => b.BucketName == bucketName).Any ();
			}
		}

        public bool CreateBucket ()
        {   
            try {
                Logger.LogInfo ("Connection","Creating a bucket "+bucketName);
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

		public void Download (SQ.Repository.File  file)
        {
            try {
                string sourcekey = file.AbsolutePath;//file.AbsolutePath.Substring (1, file.AbsolutePath.Length-1);
                Logger.LogInfo ("Connection", "Download the file " + sourcekey + ".");
			
                GetObjectRequest objectRequest = new GetObjectRequest (){
                BucketName = bucketName,
				Key = sourcekey
			};

                using (GetObjectResponse response = Connect ().GetObject (objectRequest)) {
                    response.WriteResponseStreamToFile (file.FullLocalName);
                }

            } catch (Exception e) {
                Logger.LogInfo ("Error", e);
            }
			
		}	

		public void Upload (SQ.Repository.File  file)
		{	
			Logger.LogInfo ("Connection","Uploading the file "+ file.FullLocalName+" to bucket.");
			if (!System.IO.File.Exists(file.FullLocalName))
				return;

            GenericUpload ( file.RelativePathInBucket,  file.Name,  file.FullLocalName);
		}

		public bool CreateFolder (Folder folder)
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
				Logger.LogInfo ("Connection","Creating folder "+name+".");
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
			} catch(Exception e) {
				Logger.LogInfo ("Connection","Fail to upload "+e.Message);
				return false;
			}
		}

		public void UploadToTrash (SQ.Repository.File  file)
		{
			Logger.LogInfo ("Connection","Uploading the file "+ file.Name+" to bucket.");
			if (!System.IO.File.Exists(file.FullLocalName))
				return;

			string key;
			if ( file.IsAFolder) {
				key =  file.Name;
			} else {
				key =  file.Name + "(0)";
			}
			Logger.LogInfo ("Connection","Uploading the file "+key+" to trash.");
			GenericUpload ( file.TrashRelativePath, key,  file.FullLocalName);	
		}

		public void Copy (SQ.Repository.File source, SQ.Repository.File destination)
		{
			//string sourcekey = source.AbsolutePath.Substring (1,source.AbsolutePath.Length-1);
			GenericCopy (bucketName, source.AbsolutePath, destination.RelativePathInBucket, destination.Name);			
		}

		public void CopyInTrash (SQ.Repository.File source, SQ.Repository.File destination)
		{
			GenericCopy (bucketName, source.TrashAbsolutePath, destination.TrashRelativePath, destination.Name);
		}
		
		public void CopyToTrash (SQ.Repository.File source)
		{
			string destinationName;
			if (source.IsAFolder)
				destinationName = source.Name;
			else
				destinationName = source.Name+"(0)";

			GenericCopy (bucketName, source.AbsolutePath, source.TrashRelativePath, destinationName);
		}

		public void Delete (SQ.Repository.File  file)
		{
            Logger.LogInfo ("Connection", "Deleting " + file.AbsolutePath + " from bucket.");
			GenericDelete (file.AbsolutePath);
		}

		public void DeleteInTrash (SQ.Repository.File file)
		{
			GenericDelete (file.TrashAbsolutePath);
		}

		private void GenericDelete (string key)
		{
			try {
				if (key == "")
					return;


				DeleteObjectRequest request = new DeleteObjectRequest (){
				BucketName = bucketName,
				Key = key
			};
				using (DeleteObjectResponse response = Connect ().DeleteObject (request)) {
					response.Dispose ();
				}
			} catch (Exception e){
				Logger.LogInfo ("Connection", e);
			}
		}

		#region Auxiliar

		public TimeSpan CalculateDiffClocks ()
		{
			try {
				SQ.Repository.File clockFile = new RemoteFile (Constant.CLOCK_TIME);
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
				
				Delete(clockFile);
				
				DateTime remoteClock = Convert.ToDateTime (sRemoteclock);
				
				return localClock.Subtract(remoteClock);
				
			} catch(Exception e) {
				return new TimeSpan(0);
				Logger.LogInfo ("Connection","Fail to determinate a remote clock: "+e.Message +" \n");
			}
		}


		public bool GetValidationCallBack (Object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate,
		                                   System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors errors)
		{
			return true;
		}


        private void GenericCopy (string sourceBucket, string sourceKey, string destinationBucket, string destinationKey)
        {
            try{
                CopyObjectRequest request = new CopyObjectRequest (){
                    DestinationBucket = destinationBucket,
                    DestinationKey = destinationKey,
                    SourceBucket = sourceBucket,
                    SourceKey = sourceKey
                };
                Connect ().CopyObject (request);
            } catch{
                //Logger.LogInfo ("Connection", "Problems sending file to trash folder");
            } 
        }

        private void GenericUpload (string bucketName, string key, string filepath)
		{
			try {
				PutObjectRequest putObject = new PutObjectRequest ()
                {
                    BucketName = bucketName,
                    FilePath = filepath,
                    Key = key, 
                    Timeout = 3600000
                };
				using (S3Response response = Connect ().PutObject (putObject)) {
					response.Dispose ();
				}
			} catch (ObjectDisposedException) {
				Logger.LogInfo ("Connection","An exception occurred, the file will be resend.");
				GenericUpload (bucketName, key, filepath);
			}
			catch(Exception e) {
                Logger.LogInfo ("Connection",e);
            }
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
			}
		}
	}
}