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


namespace GreenQloud
{
	public class StorageQloudRepo : RemoteRepo
	{
        string bucketName;
		public StorageQloudRepo ()
		{

		}


        #region RemoteRepo



        public bool InitBucket ()
        {
            if (!ExistsBucket)
                return CreateBucket();
            
            else return true; 
        }

        public  bool ExistsTrashFolder {
            get {
                return  TrashFiles.Any(rm => IsTrashFile(rm));
            }
        }

        
        public bool InitTrashFolder ()
        {
            if (!ExistsTrashFolder) {
                return CreateTrashFolder ();
            }
            else return true;
        }
        
        
        public override TransferResponse Upload (StorageQloudObject file)
        {
            CurrentTransfer = new TransferResponse(file, TransferType.UPLOAD);
            if (!System.IO.File.Exists(file.FullLocalName))
            {
                CurrentTransfer.Status = TransferStatus.DONE_WITH_ERROR;
                Logger.LogInfo ("Upload", string.Format("Could not upload {0} because it does not exist in local repository.",file.AbsolutePath));
                return CurrentTransfer; 
            }
            
            GenericUpload ( file.RelativePathInBucket,  file.Name,  file.FullLocalName);
            return CurrentTransfer;
        }
        

        public override TransferResponse Copy (StorageQloudObject source, StorageQloudObject destination)
        {
            CurrentTransfer = new TransferResponse(destination, GreenQloud.TransferType.UPLOAD);
            if (source.InTrash)
               GenericCopy (bucketName, source.TrashAbsolutePath, destination.RelativePathInBucket, destination.Name);
            else
               GenericCopy (bucketName, source.AbsolutePath, destination.RelativePathInBucket, destination.Name);
            Logger.LogInfo ("Connection", "Copy is done");
            return CurrentTransfer;
        }
        
        public void CopyToTrashFolder (StorageQloudObject source, StorageQloudObject destination)
        {
            GenericCopy (bucketName, source.AbsolutePath, destination.TrashRelativePath, destination.Name);
        }
        
        public void Move (StorageQloudObject source, StorageQloudObject destination)
        {
            string destinationName;
            if (source.IsAFolder)
                destinationName = source.Name;
            else
                destinationName = source.Name+"(0)";
            
            GenericCopy (bucketName, source.AbsolutePath, source.TrashRelativePath, destinationName);
            if (Files.Any (rf => rf.Name == source.Name))
                GenericDelete (source.AbsolutePath);
        }
        
        public override TransferResponse MoveFileToTrash (StorageQloudObject  file)
        {
            CurrentTransfer = new TransferResponse (file, TransferType.REMOVE);
            string destinationName;         
            destinationName = file.Name + "(1)";
            
            UpdateTrashFolder (file);
            
            
            GenericCopy (bucketName, file.AbsolutePath, file.TrashRelativePath, destinationName);
            GenericDelete (file.AbsolutePath);
            CurrentTransfer.EndTime= DateTime.Now;
            return CurrentTransfer;
        }
        
        public override TransferResponse MoveFolderToTrash (StorageQloudObject folder)
        {
            CurrentTransfer = new TransferResponse (folder, TransferType.REMOVE);
            GenericCopy (bucketName, folder.AbsolutePath+"/", folder.TrashRelativePath, folder.Name+"/");           
            
            
            GenericDelete (folder.AbsolutePath+"/");
            return CurrentTransfer;
        }


        
        public bool IsTrashFile (StorageQloudObject file)
        {
            return file.InTrash;
        }
        
        public override TransferResponse SendLocalVersionToTrash (StorageQloudObject file)
        {
            CurrentTransfer = new TransferResponse(file, GreenQloud.TransferType.REMOVE);
            if (!System.IO.File.Exists (file.FullLocalName)) {
                CurrentTransfer.Status = TransferStatus.DONE_WITH_ERROR;
            }
            string key;
            if ( file.IsAFolder) {
                key =  file.Name;
            } else {
                key =  file.Name + "(0)";
            }
            Logger.LogInfo ("Connection","Uploading the file "+key+" to trash.");
            GenericUpload ( file.TrashRelativePath, key,  file.FullLocalName);
            Logger.LogInfo ("Connection","File "+file.Name+" was sent to trash folder.");
            UpdateTrashFolder (file);
            
            return CurrentTransfer;
        }
        
        public bool SendRemoteVersionToTrash (StorageQloudObject file)
        {
            string destinationName;
            if (file.IsAFolder)
                destinationName = file.Name;
            else
                destinationName = file.Name+"(0)";
            
            GenericCopy (bucketName, file.AbsolutePath, file.TrashRelativePath, destinationName);
            bool copySucessfull =  TrashFiles.Any (rm => file.AbsolutePath+"(0)" == rm.AbsolutePath);
            if (copySucessfull)
                UpdateTrashFolder (file);
            
            return copySucessfull;
        }
        
        public void UpdateTrashFolder (StorageQloudObject  file)
        {
            if (file.IsAFolder)
                return;
            List<StorageQloudObject> versions = TrashFiles.Where (tf => tf.AbsolutePath != string.Empty && tf.AbsolutePath.Substring(0, tf.AbsolutePath.Length-3)== file.AbsolutePath).OrderByDescending(t => t.AbsolutePath).ToList<StorageQloudObject> ();
            
            int overload = versions.Count-2;
            for (int i=0; i<overload; i++) {
                StorageQloudObject v = versions[i];
                GenericDelete(v.TrashAbsolutePath);
                versions.Remove(v);
            }
            
            foreach (StorageQloudObject version in versions) {
                if(version.AbsolutePath == string.Empty)
                    continue;
                int lastOpenParenthesis = version.AbsolutePath.LastIndexOf ("(")+1;
                int lastCloseParenthesis = version.AbsolutePath.LastIndexOf (")");
                int versionNumber = int.Parse (version.AbsolutePath.Substring (lastOpenParenthesis, lastCloseParenthesis - lastOpenParenthesis));
                versionNumber++;
                string newName = string.Format ("{0}{1})", version.AbsolutePath.Substring (0, lastOpenParenthesis), versionNumber);
                StorageQloudObject newVersion = new StorageQloudObject (newName);
                GenericCopy (bucketName, version.TrashAbsolutePath, newVersion.TrashRelativePath, newVersion.Name);
                GenericDelete (version.TrashAbsolutePath);
            }
            
        }      

        
        private List<StorageQloudObject> GetVersionsOrderByLastModified (StorageQloudObject  file)  {
            return  TrashFiles.Where (ft => ft.AbsolutePath.Contains (file.AbsolutePath)).OrderBy(ft => ft.AsS3Object.LastModified).ToList<StorageQloudObject>();
        }


        #endregion

        
        #region Files
        public override List<StorageQloudObject> Files{ 
            get {
                return GetStorageQloudObjects (GetFiles ().Where(sobj => !sobj.Key.Contains(".trash/") && sobj.Key != Constant.CLOCK_TIME && !sobj.Key.EndsWith("/")).ToList());
            } 
        }
        
        public override List<StorageQloudObject> Folders{ 
            get {
                return GetStorageQloudObjects (GetFiles ().Where(sobj => !sobj.Key.Contains(".trash/") && sobj.Key.EndsWith("/")).ToList());
            } 
        }
        
        public override List<StorageQloudObject> AllFiles{ 
            get {
                return GetStorageQloudObjects (GetFiles());
            } 
        }
        
        protected List<StorageQloudObject> GetStorageQloudObjects (List<S3Object> files)
        {
            List <StorageQloudObject> remoteFiles = new List <StorageQloudObject> ();
            
            foreach (S3Object file in files) {
                remoteFiles.Add (new StorageQloudObject (file));
            }
            return remoteFiles;
        }
        
        public override List<StorageQloudObject> TrashFiles {
            get{
                return AllFiles.Where(f => IsTrashFile(f)).ToList();
            } 
        }
        
        
        #endregion


		#region Connection

        private AmazonS3Client connection;       

        public static new void Authenticate (string username, string password)
        {
            Uri uri = new Uri (GlobalSettings.AuthenticationURL);
        
            WebRequest myReq = WebRequest.Create (uri);
            string usernamePassword = username + ":" + password;
            CredentialCache mycache = new CredentialCache ();
            mycache.Add (uri, "Basic", new NetworkCredential (username, password));
            myReq.Credentials = mycache;
            myReq.Headers.Add ("Authorization", "Basic " + Convert.ToBase64String (new ASCIIEncoding ().GetBytes (usernamePassword)));
            using (WebResponse wr = myReq.GetResponse ()){
                Stream receiveStream = wr.GetResponseStream ();
                StreamReader reader = new StreamReader (receiveStream, Encoding.UTF8);
                string receiveContent = reader.ReadToEnd ();
                Newtonsoft.Json.Linq.JObject o = Newtonsoft.Json.Linq.JObject.Parse(receiveContent);               
                Credential.SecretKey = (string)o["api_private_key"];
                Credential.PublicKey = (string)o["api_public_key"];
            }
            Logger.LogInfo ("Authetication", "Keys loaded");
        }   
        
        public AmazonS3Client Connect ()
        {
            if (connection != null) {
                return connection;
            }
            try {    
                bucketName = DefaultBucketName;
                AmazonS3Config config = CreateConfig ();
                connection = (AmazonS3Client)Amazon.AWSClientFactory.CreateAmazonS3Client (Credential.PublicKey, Credential.SecretKey, config);
                Logger.LogInfo("Connection", "Start a new connection");
                return connection;
            }catch (System.Net.WebException e){
                if (e.Status == WebExceptionStatus.NameResolutionFailure || e.Status == WebExceptionStatus.Timeout){
                    throw new DisconnectionException();
                }else{                    
                    if (((HttpWebResponse)e.Response).StatusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new AccessDeniedException(); 
                    }
                }
            }catch(AmazonS3Exception s3e)
            {
                if (s3e.StatusCode == HttpStatusCode.Forbidden)
                    throw new AccessDeniedException(); 
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
            try{
                List<S3Object> files = Reconnect().ListObjects (
                    new ListObjectsRequest ().WithBucketName (bucketName)
                    ).S3Objects;
                return files;
            }catch (System.Net.WebException e){
                if (e.Status == WebExceptionStatus.NameResolutionFailure || e.Status == WebExceptionStatus.Timeout){
                    throw new DisconnectionException();
                }else{                    
                    if (((HttpWebResponse)e.Response).StatusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new AccessDeniedException(); 
                    }
                }
            }catch(AmazonS3Exception s3e)
            {
                if (s3e.StatusCode == HttpStatusCode.Forbidden)
                    throw new AccessDeniedException(); 
            }
            return null;
        }

        public override TransferResponse Download (StorageQloudObject  file)
        {        
            CurrentTransfer = new TransferResponse(file, TransferType.DOWNLOAD);
            GetObjectResponse response = null;
            try {
                string sourcekey = file.AbsolutePath;
                Logger.LogInfo ("Connection", "Download the file " + sourcekey + ".");
			
                GetObjectRequest objectRequest = new GetObjectRequest (){
                    BucketName = bucketName,
    				Key = sourcekey
			    };
                long lastTransferredBytes = 0;
                CurrentTransfer.InitialTime = DateTime.Now;
                using (response = Connect().GetObject(objectRequest))
                {
                    response.WriteObjectProgressEvent += (object sender, WriteObjectProgressArgs e) => {
                        CurrentTransfer.TransferredBits += e.TransferredBytes-lastTransferredBytes;
                        lastTransferredBytes = e.TransferredBytes;
                    };
                    
                    response.WriteResponseStreamToFile(file.FullLocalName);
                }
                CurrentTransfer.EndTime = DateTime.Now;
            }catch(System.Threading.ThreadInterruptedException){
            }
            catch (Exception e) {
                Logger.LogInfo ("Error", e.Message);
                CurrentTransfer.Status = TransferStatus.DONE_WITH_ERROR;
                CurrentTransfer.EndTime = DateTime.Now;
            } finally {
                if (response!=null) response.Dispose();
            }
            return CurrentTransfer;
		}	



        public override void DownloadFull (StorageQloudObject  file)
        {        
            GetObjectResponse response = null;
            try {
                string sourcekey = file.AbsolutePath;
                CurrentTransfer = new TransferResponse();
                CurrentTransfer.StorageQloudObject = file;
                Logger.LogInfo ("RemoteRepo", "Download the file " + sourcekey + ".");
                
                GetObjectRequest objectRequest = new GetObjectRequest (){
                    BucketName = bucketName,
                    Key = sourcekey
                };
                long lastTransferredBytes = 0;
                using (response = Connect().GetObject(objectRequest))
                {
                    response.WriteObjectProgressEvent += (object sender, WriteObjectProgressArgs e) => {
                        FullSizeTransfer += e.TransferredBytes-lastTransferredBytes;

                        lastTransferredBytes = e.TransferredBytes;
                        Logger.LogInfo ("RemoteRepo",string.Format("Downloading {0} of {1} bytes", e.TransferredBytes, e.TotalBytes));
                    };
                    
                    response.WriteResponseStreamToFile(file.FullLocalName);
                }
            } catch (Exception e) {
                Logger.LogInfo ("Error", e.Message);
            } finally {
                if (response!=null) response.Dispose();
            }
        }

        public override TransferResponse CreateFolder (StorageQloudObject folder)
		{
            CurrentTransfer = new TransferResponse(folder, TransferType.CREATEREMOTEFOLDER);
			CreateFolder (folder.Name, folder.RelativePathInBucket);
            return CurrentTransfer;
		}

		public bool CreateTrashFolder ()
		{
			return CreateFolder (Constant.TRASH, bucketName);
		}

		private bool CreateFolder (string name, string relativePath)
		{
            S3Response response; 
			try {
                Logger.LogInfo ("Connection","Creating folder "+name+".");
                if (!name.EndsWith("/"))
                    name+="/";
				PutObjectRequest putObject = new PutObjectRequest (){
					BucketName = relativePath,
					Key = name,
					ContentBody = string.Empty
				};
                CurrentTransfer.InitialTime = DateTime.Now;
				using (response = Connect ().PutObject (putObject))
				{
									
				}

                CurrentTransfer.EndTime = DateTime.Now;

                Logger.LogInfo ("Connection", "Folder "+name+" created");
            }catch (AmazonS3Exception) {
                if(InitializeBucket())
                  return  CreateFolder (name, relativePath);
                else{
                    Logger.LogInfo ("Connection", "There is a problem of comunication and the file will be sent back.");
                    CurrentTransfer.Status = TransferStatus.DONE_WITH_ERROR;
                    CurrentTransfer.EndTime = DateTime.Now;
                    return false;
                }
            }
            catch(Exception e) {
				Logger.LogInfo ("Connection","Fail to upload "+e.Message);
                CurrentTransfer.Status = TransferStatus.DONE_WITH_ERROR;
                CurrentTransfer.EndTime = DateTime.Now;
				return false;
			}
            finally{
                response.Dispose();
            }
            return true;

           
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
                CurrentTransfer.InitialTime = DateTime.Now;
                using (DeleteObjectResponse response = connection.DeleteObject (request)) {
                    response.Dispose ();
				}
                CurrentTransfer.EndTime = DateTime.Now;
                if(key!=Constant.CLOCK_TIME)
                Logger.LogInfo ("Connection", string.Format("{0} was deleted in bucket.", key));
                CurrentTransfer.Status = TransferStatus.DONE;
            } catch (AmazonS3Exception) {
                if(InitializeBucket())
                    GenericDelete (key);
                else{
                    CurrentTransfer.EndTime = DateTime.Now;
                    Logger.LogInfo ("Connection", "There is a problem of comunication and the file will be sent back.");
                    CurrentTransfer.Status = TransferStatus.DONE_WITH_ERROR;
                }
            }catch (Exception e){
                CurrentTransfer.EndTime = DateTime.Now;
                CurrentTransfer.Status = TransferStatus.DONE_WITH_ERROR;
				Logger.LogInfo ("Connection", e);
			}
		}

		#region Auxiliar
        DateTime lastDiffClock = new DateTime();
        TimeSpan diff = new TimeSpan();
		public TimeSpan CalculateDiffClocks ()
		{
			try {
                if (DateTime.Now.Subtract(lastDiffClock).TotalMinutes>30){

                    StorageQloudObject clockFile = new StorageQloudObject (Constant.CLOCK_TIME);
    				PutObjectRequest putObject = new PutObjectRequest ()
    				{
                        BucketName = DefaultBucketName,
    					Key = clockFile.Name,
    					ContentBody = string.Empty
    				};
    				
    				DateTime localClock;
    				using (S3Response response = Connect ().PutObject (putObject)){
    					localClock = DateTime.Now;
    					response.Dispose ();
    				}
    				
    				ListObjectsResponse files = Connect ().ListObjects (new ListObjectsRequest ().WithBucketName (DefaultBucketName));
    				S3Object remotefile = files.S3Objects.Where (o => o.Key == clockFile.Name).FirstOrDefault();
    				string sRemoteclock = remotefile.LastModified;
    				
                    GenericDelete (clockFile.AbsolutePath);

    				DateTime remoteClock = Convert.ToDateTime (sRemoteclock);
                    diff = localClock.Subtract(remoteClock);
                    lastDiffClock = localClock;
                }
                return diff;
				
			} catch(Exception e) {
                Logger.LogInfo ("Connection","Fail to determinate a remote clock: "+e.Message +" \n");

                return new TimeSpan (0);
			}
		}

        public override TimeSpan DiffClocks 
        {
            get{
                return CalculateDiffClocks();
            }
        }


        private void GenericCopy (string sourceBucket, string sourceKey, string destinationBucket, string destinationKey)
        {
            try {
                CurrentTransfer.InitialTime = DateTime.Now;
                CopyObjectRequest request = new CopyObjectRequest (){
                    DestinationBucket = destinationBucket,
                    DestinationKey = destinationKey,
                    SourceBucket = sourceBucket,
                    SourceKey = sourceKey
                };

                using (CopyObjectResponse cor = Connect ().CopyObject (request)){}
                CurrentTransfer.EndTime = DateTime.Now;
            } 
            //TODO Understand why System.InvalidOperationException is catched, and the file is copied
            catch{
                               
            }
        }

        private void GenericUpload (string bucketName, string key, string filepath)
        {
            S3Response response = new S3Response();
            AmazonS3Client upconnection;
            try {
                AmazonS3Config config = CreateConfig ();
                upconnection = (AmazonS3Client)Amazon.AWSClientFactory.CreateAmazonS3Client (Credential.PublicKey, Credential.SecretKey, config);
               
                PutObjectRequest putObject = new PutObjectRequest ()
                {
                    BucketName = bucketName,
                    FilePath = filepath,
                    Key = key, 
                    Timeout = GlobalSettings.UploadTimeout
                };                
                CurrentTransfer.InitialTime = DateTime.Now;
                using (response = upconnection.PutObject (putObject)) {
                    response.Dispose ();
                }
                CurrentTransfer.EndTime = DateTime.Now;
                Logger.LogInfo ("Connection", string.Format ("{0} was uploaded.", filepath));

                CurrentTransfer.Status = TransferStatus.DONE;
            } catch (ObjectDisposedException) {
                Logger.LogInfo ("Connection", "An exception occurred, the file will be resend.");
                GenericUpload (bucketName, key, filepath);
            } catch (AmazonS3Exception) {

                if (InitializeBucket ())
                    GenericUpload (bucketName, key, filepath);
                else{
                    Logger.LogInfo ("Connection", "There is a problem of comunication and the file will be sent back.");
                    CurrentTransfer.Status = TransferStatus.DONE_WITH_ERROR;
                }
            } catch (Exception e) {
                Logger.LogInfo ("Connection", e);
                CurrentTransfer.Status = TransferStatus.DONE_WITH_ERROR;
            } finally {
                response.Dispose();
                upconnection.Dispose();
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

        public override void UpdateStorageQloud ()
        {
            S3Response response = new S3Response ();
            try {
                PutObjectRequest putObject = new PutObjectRequest ()
                {
                    BucketName = DefaultBucketName,
                    Key = Constant.CLOCK_TIME,
                    ContentBody = string.Empty
                };
                using (response = Connect ().PutObject (putObject)) {
                    response.Dispose ();
                }
                GenericDelete (Constant.CLOCK_TIME);
            } catch {
            }
        }

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

        #region implemented abstract members of RemoteRepo

        public override bool ExistsVersion (StorageQloudObject file)
        {
            return TrashFiles.Any (tf => tf.AbsolutePath == file.AbsolutePath+"(1)");
        }

        public override TransferResponse Delete (StorageQloudObject request)
        {
            CurrentTransfer = new TransferResponse(request, TransferType.REMOVE);
            GenericDelete (request.AbsolutePath);
            return CurrentTransfer;
        }

        public override bool ExistsFolder (StorageQloudObject folder)
        {
            return Folders.Any (rf => rf.AbsolutePath==folder.AbsolutePath || rf.AbsolutePath.Contains(folder.AbsolutePath));
        }
        
        public override bool Exists (StorageQloudObject file)
        {
            return Files.Any (rf => rf.AbsolutePath == file.AbsolutePath);
        }


        public override List<StorageQloudObject> GetCopys (StorageQloudObject file)
        {
            return AllFiles.Where (rf => rf.RemoteMD5Hash == file.LocalMD5Hash && rf.AbsolutePath != file.AbsolutePath && !rf.Name.EndsWith("/") && rf.AsS3Object.Size>0).ToList<StorageQloudObject> ();
        }

        #endregion
	}
}