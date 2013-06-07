using System;
using GreenQloud.Model;
using System.Collections.Generic;
using Amazon.S3.Model;
using Amazon.S3;
using System.Net;
using System.IO;
using System.Text;
using System.Linq;
using GreenQloud.Util;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace GreenQloud.Repository.Remote
{
    //TODO refactor md5 to eTag.. missmatch names
    public class StorageQloudRemoteRepositoryController : RemoteRepositoryController
    {
        Local.StorageQloudPhysicalRepositoryController physicalController = new Local.StorageQloudPhysicalRepositoryController();
        public StorageQloudRemoteRepositoryController ()
        {
        }

        #region implemented abstract members of RemoteRepositoryController

        public override List<GreenQloud.Model.RepositoryItem> GetCopys (GreenQloud.Model.RepositoryItem item)
        {
            return AllItems.Where (rf => rf.RemoteETAG == item.LocalETAG && rf.AbsolutePath != item.AbsolutePath).ToList<RepositoryItem> ();
        }

        public override bool ExistsVersion (GreenQloud.Model.RepositoryItem item)
        {

            string absolutePath = item.AbsolutePath;
            if (!item.IsAFolder)
                absolutePath += "(1)";

            return TrashItems.Any (tf => tf.AbsolutePath == absolutePath);
        }

        public override Transfer Download (RepositoryItem item)
        {
            CurrentTransfer = new Transfer (item, TransferType.DOWNLOAD);
            if (item.IsAFolder){
                physicalController.CreateFolder(item);
            }else{
                GetObjectResponse response = null;

                    string sourcekey = item.AbsolutePath;
                   
                    Logger.LogInfo ("Connection", "Download the file " + sourcekey + ".");
                    
                    GetObjectRequest objectRequest = new GetObjectRequest (){
                        BucketName = RuntimeSettings.DefaultBucketName,
                        Key = sourcekey
                    };

                    long lastTransferredBytes = 0;
                    CurrentTransfer.InitialTime = GlobalDateTime.Now;
                    using (response = Connect().GetObject(objectRequest))
                    {
                        response.WriteObjectProgressEvent += (object sender, WriteObjectProgressArgs e) => {
                            CurrentTransfer.TransferredBits += e.TransferredBytes-lastTransferredBytes;
                            lastTransferredBytes = e.TransferredBytes;
                            CurrentTransfer.TotalSize = e.TotalBytes;
                        };
                        
                        response.WriteResponseStreamToFile(item.FullLocalName);
                    }
                    response.Dispose ();
                    CurrentTransfer.EndTime = GlobalDateTime.Now;
            }
            return CurrentTransfer;
        }

        public override Transfer Upload (RepositoryItem item)
        {
            if(item.IsAFolder){
                return CreateFolder (item);
            }else{
                CurrentTransfer = new Transfer (item, TransferType.UPLOAD);

                /*if (!physicalController.Exists(item))
                {
                    CurrentTransfer.Status = TransferStatus.DONE_WITH_ERROR;
                    Logger.LogInfo ("Upload", string.Format("Could not upload {0} because it does not exist in local repository.", item.AbsolutePath));
                    return CurrentTransfer; 
                }*/

                GenericUpload ( item.RelativePathInBucket,  item.Name,  item.FullLocalName);
            }
            return CurrentTransfer;
        }

        public override Transfer Move (RepositoryItem item)
        {
            /*TODO after api move works
            CurrentTransfer = new Transfer (item, TransferType.REMOTE_MOVE); 

            GenericCopy (RuntimeSettings.DefaultBucketName, item.AbsolutePath, RuntimeSettings.DefaultBucketName + Path.DirectorySeparatorChar + item.ResultObjectFolder, item.ResultObjectName);
            Delete (item);
            CurrentTransfer.EndTime = GlobalDateTime.Now;*/

            CurrentTransfer = new Transfer (item, TransferType.LOCAL_MOVE);
            GenericUpload (item.RelativeResultObjectPathInBucket,  item.ResultObjectName,  item.FullLocalResultObject);
            CurrentTransfer = Delete(item);

            return CurrentTransfer;
        }

        //TODO refactor after move works
        public override Transfer MoveToTrash (RepositoryItem item)
        {
            CurrentTransfer = new Transfer (item, TransferType.LOCAL_REMOVE);

            //string pathTrash = item.TrashAbsolutePath + "("+GlobalDateTime.NowUniversalString+")";
            //GenericUpload (item.RelativePathInBucket, item.FullLocalName ,  pathTrash);
            CurrentTransfer = Delete(item);
            return CurrentTransfer;

            /*item.ResultObjectRelativePath = item.TrashAbsolutePath;
            item.ResultObjectRelativePath = item.ResultObjectRelativePath +"("+GlobalDateTime.NowUniversalString+")";
            return  Move (item);*/
        }

        public override Transfer Delete (RepositoryItem item)
        {
            CurrentTransfer = new Transfer (item, TransferType.REMOTE_REMOVE);
            GenericDelete (item.AbsolutePath);
            return CurrentTransfer;
        }

        public override Transfer SendLocalVersionToTrash (RepositoryItem item)
        {
            CurrentTransfer = new Transfer (item, TransferType.REMOTE_REMOVE);
            if (!physicalController.Exists(item)) {
                CurrentTransfer.Status = TransferStatus.DONE_WITH_ERROR;
            }
            string key;
            if ( item.IsAFolder) {
                CreateFolderInTrash(item);
            } else {
                key =  item.Name + "(1)";
            
               // UpdateTrashFolder (item);
                Logger.LogInfo ("Connection","Uploading the file "+key+" to trash.");
                GenericUpload ( item.TrashRelativePath, key,  item.FullLocalName);
                Logger.LogInfo ("Connection","File "+item.Name+" was sent to trash folder.");

            }
            return CurrentTransfer;
        }

        public override Transfer CreateFolder (RepositoryItem item)
        {
            CurrentTransfer = new Transfer(item, TransferType.REMOTE_CREATE_FOLDER);
            CreateFolder (item.Name, item.RelativePathInBucket);
            return CurrentTransfer;
        }

        public override Transfer Copy (GreenQloud.Model.RepositoryItem source, GreenQloud.Model.RepositoryItem destination)
        {
            CurrentTransfer = new Transfer(destination, TransferType.UPLOAD);
            if (source.InTrash)
                GenericCopy (RuntimeSettings.DefaultBucketName, source.TrashAbsolutePath, destination.RelativePathInBucket, destination.Name);
            else
                GenericCopy (RuntimeSettings.DefaultBucketName, source.AbsolutePath, destination.RelativePathInBucket, destination.Name);
            Logger.LogInfo ("Connection", "Copy is done");
            return CurrentTransfer;
        }


        public override bool Exists (GreenQloud.Model.RepositoryItem item)
        {
            listItems = Items;
            return listItems.Any (rf => rf.AbsolutePath == item.AbsolutePath);
        }

        public override bool ExistsCopies (GreenQloud.Model.RepositoryItem item)
        {
            return AllItems.Any(rf => rf.RemoteETAG == item.RemoteETAG && rf.AbsolutePath != item.AbsolutePath);
        }

        public override string RemoteETAG (string path)
        {
            GetObjectMetadataRequest request = new GetObjectMetadataRequest { 
                BucketName = RuntimeSettings.DefaultBucketName,
                Key = path
            };
            GetObjectMetadataResponse met;
            using ( met = Connect ().GetObjectMetadata (request)){}
            met.Dispose ();
            return met.ETag;
        }

        public override List<GreenQloud.Model.RepositoryItem> AllItems {
            get {
                return GetInstancesOfItems (GetS3Objects());
            }
        }

        List<RepositoryItem> listItems;

        public override List<GreenQloud.Model.RepositoryItem> Items {
            get {
                listItems = GetInstancesOfItems (GetS3Objects().Where(i => !i.Key.Contains(Constant.TRASH) && i.Key != Constant.CLOCK_TIME).ToList());
                return listItems;
            }
        }

        public override List<GreenQloud.Model.RepositoryItem> TrashItems {
            get {
                return GetInstancesOfItems (GetS3Objects().Where(i => i.Key.Contains(Constant.TRASH)).ToList());
            }
        }

        public override List<GreenQloud.Model.RepositoryItem> RecentChangedItems (DateTime LastSyncTime) {


            TimeSpan diffClocks = DiffClocks;  

            DateTime referencialClock = LastSyncTime.Subtract (diffClocks);   
            List<S3Object> listS3O = GetS3Objects();

            List<RepositoryItem> list = GetInstancesOfItems (listS3O.Where (rf => (Convert.ToDateTime (rf.LastModified).Subtract (referencialClock).TotalSeconds > 0 || rf.Key.EndsWith("/")) && !rf.Key.Contains(Constant.TRASH)).ToList());

            return list;
        }

        #endregion

        #region Generic
        private void GenericCopy (string sourceBucket, string sourceKey, string destinationBucket, string destinationKey)
        {
            try{
                CurrentTransfer.InitialTime = GlobalDateTime.Now;
                CopyObjectRequest request = new CopyObjectRequest (){
                    DestinationBucket = destinationBucket,
                    DestinationKey = destinationKey,
                    SourceBucket = sourceBucket,
                    SourceKey = sourceKey
                };

                using (CopyObjectResponse cor = Connect ().CopyObject (request)){
                    cor.Dispose ();
                }
                CurrentTransfer.EndTime = GlobalDateTime.Now;

            //TODO WHY EVER OCCUR THIS ERROR?????
            } catch (Exception e){
                Logger.LogInfo ("Expected Error found...", e.StackTrace);
            }
        }

        public void GenericDelete (string key)
        {
            if (key == "")
                return;
            
            DeleteObjectRequest request = new DeleteObjectRequest (){
                BucketName = RuntimeSettings.DefaultBucketName,
                Key = key
            };
            AmazonS3Client connection = Connect();
            
            if(key!=Constant.CLOCK_TIME)  
            CurrentTransfer.InitialTime = GlobalDateTime.Now;

            using (DeleteObjectResponse response = connection.DeleteObject (request)) {

            }

            if(key!=Constant.CLOCK_TIME)  {              
                Logger.LogInfo ("Connection", string.Format("{0} was deleted in bucket.", key));

                CurrentTransfer.EndTime = GlobalDateTime.Now;
                CurrentTransfer.Status = TransferStatus.DONE;
            }

        }

        bool GetValidationCallBack (object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            X509Certificate2 certificate2 = new X509Certificate2 (certificate.GetRawCertData ());

            string cert_fingerprint = "B79445CC7B5EBC812E0BAE00BF52D3E95D32A116";
            
            
            if (!certificate2.Thumbprint.Equals (cert_fingerprint)) {
                Console.WriteLine (certificate2.FriendlyName);
                Logger.LogInfo ("Controller", string.Format("Invalid certificate: {0} - {1}",cert_fingerprint, certificate2.Thumbprint));
                
                return false;
                
            }
            
            
            return true;
        }
        
        private void GenericUpload (string bucketName, string key, string filepath)
        {
            if (physicalController.Exists(filepath)) {
                S3Response response = new S3Response ();
                AmazonS3Client upconnection;

                AmazonS3Config config = CreateConfig ();
                upconnection = (AmazonS3Client)Amazon.AWSClientFactory.CreateAmazonS3Client (Credential.PublicKey, Credential.SecretKey, config);
                
                PutObjectRequest putObject = new PutObjectRequest () {
                    BucketName = bucketName,
                    FilePath = filepath,
                    Key = key, 
                    Timeout = GlobalSettings.UploadTimeout
                };                
                CurrentTransfer.InitialTime = GlobalDateTime.Now;
                using (response = upconnection.PutObject (putObject)) {

                }
                CurrentTransfer.EndTime = GlobalDateTime.Now;
                Logger.LogInfo ("Connection", string.Format ("{0} was uploaded.", filepath));
                CurrentTransfer.Status = TransferStatus.DONE;
                UpdateStorageQloud ();
            } else {
                throw new AbortedOperationException ("File doesn't exists. "+filepath);
            }
        }
        #endregion

        #region Handle S3Objects

        public RepositoryItem CreateObjectInstance (S3Object s3item)
        {
            LocalRepository repo;
            if (s3item.Key.Contains ("/")){
                string root = s3item.Key.Substring(0, s3item.Key.IndexOf ("/"));
                repo =  new Persistence.SQLite.SQLiteRepositoryDAO().GetRepositoryByRootName (string.Format("/{0}/",root));
            }
            else{
                repo = new LocalRepository(RuntimeSettings.HomePath);
            }
            RepositoryItem item = RepositoryItem.CreateInstance (repo,
                                                                 s3item.Key, false, s3item.Size, s3item.LastModified);
            item.RemoteETAG = s3item.ETag;
            item.InTrash = s3item.Key.Contains (Constant.TRASH);
            return item;
        }

        AmazonS3 client;
        public List<S3Object> GetS3Objects ()
        {
           // ServicePointManager.ServerCertificateValidationCallback = GetValidationCallBack;
            using (client = Amazon.AWSClientFactory.CreateAmazonS3Client(
                Credential.PublicKey, Credential.SecretKey, CreateConfig()))
            {
                ++Controller.Contador;
                ListObjectsRequest request = new ListObjectsRequest();
                request = new ListObjectsRequest();
                request.BucketName = RuntimeSettings.DefaultBucketName;
                List<S3Object> list;
                do
                {
                    ListObjectsResponse response = client.ListObjects(request);
                   
                    list = response.S3Objects;
                   
                    // If response is truncated, set the marker to get the next 
                    // set of keys.
                    if (response.IsTruncated)
                    {                           
                        request.Marker = response.NextMarker;
                    }
                    else
                    {
                        request = null;
                    }
                } while (request != null);

                return list;
            }
        }

        protected List<RepositoryItem> GetInstancesOfItems (List<S3Object> s3items)
        {
            List <RepositoryItem> remoteItems = new List <RepositoryItem> ();
            
            foreach (S3Object s3item in s3items) {
                if (s3item.Key != Constant.TRASH)
                {    
                    remoteItems.Add ( CreateObjectInstance (s3item));
                    //add folders that have items to persist too                   
                }
            }
            return remoteItems;
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
            AmazonS3Config config = CreateConfig ();
            connection = (AmazonS3Client)Amazon.AWSClientFactory.CreateAmazonS3Client (Credential.PublicKey, Credential.SecretKey, config);
            Logger.LogInfo("Connection", "Start a new connection"); 
            return connection;
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

        #region Auxiliar
        DateTime lastDiffClock = new DateTime();
        TimeSpan diff = new TimeSpan();
        public TimeSpan CalculateDiffClocks ()
        {
            if (GlobalDateTime.Now.Subtract(lastDiffClock).TotalMinutes>30){
                
                RepositoryItem clockFile = new RepositoryItem ();
                clockFile.Name = Constant.CLOCK_TIME;
                clockFile.RelativePath = string.Empty;
                clockFile.Repository = new LocalRepository (RuntimeSettings.HomePath);
               
                PutObjectRequest putObject = new PutObjectRequest ()
                {
                    BucketName = DefaultBucketName,
                    Key = clockFile.Name,
                    ContentBody = string.Empty
                };
                
                DateTime localClock;
                using (S3Response response = Connect ().PutObject (putObject)){
                    localClock = GlobalDateTime.Now;
                }
                ListObjectsResponse files = Connect ().ListObjects (new ListObjectsRequest ().WithBucketName (DefaultBucketName));
                S3Object remotefile = files.S3Objects.Where (o => o.Key == clockFile.Name).FirstOrDefault();
                string sRemoteclock = remotefile.LastModified;
                GenericDelete (clockFile.AbsolutePath);
                DateTime remoteClock = Convert.ToDateTime (sRemoteclock);
                diff = localClock.Subtract(remoteClock);
                lastDiffClock = localClock;
                files.Dispose ();
            }
            return diff;

        }

        public bool IsNotEmptyFolder (RepositoryItem item)
        {
            if (!item.IsAFolder)
                return false;
            return Items.Count (i=> i.AbsolutePath.Contains (item.AbsolutePath)) > 1;
        }

        List<RepositoryItem> FilesInFolder (RepositoryItem item)
        {
            return Items.Where (i => i.AbsolutePath.Contains (item.AbsolutePath) && i.AbsolutePath != item.AbsolutePath).ToList();
        }
        
        public override TimeSpan DiffClocks 
        {
            get{
                return CalculateDiffClocks();
            }
        }

        protected bool InitializeBucket ()
        {
            if (!ExistsBucket) {
                if (CreateBucket ())
                    return CreateTrashFolder ();
            } else {
                if (!GetS3Objects().Any(s3o => s3o.Key.Contains(GlobalSettings.Trash)))
                    return CreateTrashFolder ();
                return true;
            }
            return false;
        }

        public bool InitBucket ()
        {
            if (!ExistsBucket)
                return CreateBucket();
            
            else return true; 
        }
        
        public  bool ExistsTrashFolder {
            get {
                return  TrashItems.Any(rm => rm.InTrash);
            }
        }

        public bool CreateTrashFolder ()
        {
            return CreateFolder (Constant.TRASH, RuntimeSettings.DefaultBucketName);
        }
        
        private bool CreateFolder (string name, string relativePath)
        {
            S3Response response;
            Logger.LogInfo ("Connection","Creating folder "+name+".");
            if (!name.EndsWith("/"))
                name+="/";
            PutObjectRequest putObject = new PutObjectRequest (){
                BucketName = relativePath,
                Key = name,
                ContentBody = string.Empty
            };
            CurrentTransfer.InitialTime = GlobalDateTime.Now;
            using (response = Connect ().PutObject (putObject))
            {
                
            }
            response.Dispose ();
            CurrentTransfer.EndTime = GlobalDateTime.Now;
            UpdateStorageQloud();
            Logger.LogInfo ("Connection", "Folder "+name+" created");
            return true;
        }

        public bool ExistsBucket 
        {
            get {
                return Reconnect().ListBuckets () .Buckets.Any (b => b.BucketName == RuntimeSettings.DefaultBucketName);
            }
        }

        public bool CreateBucket ()
        {   
            PutBucketRequest request = new PutBucketRequest ();
            request.BucketName = RuntimeSettings.DefaultBucketName;
            Connect ().PutBucket (request);
            Logger.LogInfo("Connection", "Bucket "+RuntimeSettings.DefaultBucketName+" was created.");
            return true;
       
        }


        public List <RepositoryItem> GetVersions(RepositoryItem item){
            if (TrashItems.Any (ti => !ti.IsAFolder && ti.AbsolutePath != string.Empty && ti.AbsolutePath.Substring(0, ti.AbsolutePath.Length-3) == item.AbsolutePath))
                return TrashItems.Where (ti => !ti.IsAFolder && ti.AbsolutePath != string.Empty && ti.AbsolutePath.Substring(0, ti.AbsolutePath.Length-3) == item.AbsolutePath).OrderByDescending(t => t.AbsolutePath).ToList<RepositoryItem> ();
            return null;
        }

        public void UpdateTrashFolder (RepositoryItem  item)
        {
            if (item.IsAFolder)
                return;
                        
            List<RepositoryItem> versions = GetVersions (item);
            if (versions == null)
                return;
            
            int overload = versions.Count-2;
            for (int i=0; i<overload; i++) {
                RepositoryItem v = versions[i];
                GenericDelete(v.TrashAbsolutePath);
                versions.Remove(v);
            }
            
            foreach (RepositoryItem version in versions) {
                if(version.Name == string.Empty)
                    continue;

                GenericCopy (RuntimeSettings.DefaultBucketName, version.TrashAbsolutePath, version.TrashRelativePath, UpdateVersionName (version.Name));
                GenericDelete (version.TrashAbsolutePath);
            }            
        } 

        public string UpdateVersionName (string versionName)
        {
            int lastOpenParenthesis = versionName.LastIndexOf ("(")+1;
            int lastCloseParenthesis = versionName.LastIndexOf (")");
            int versionNumber = int.Parse (versionName.Substring (lastOpenParenthesis, lastCloseParenthesis - lastOpenParenthesis));
            versionNumber++;

            return string.Format ("{0}{1})", versionName.Substring (0, lastOpenParenthesis), versionNumber);
        }

        public void UpdateStorageQloud ()
        {
            S3Response response = new S3Response ();

            PutObjectRequest putObject = new PutObjectRequest ()
            {
                BucketName = DefaultBucketName,
                Key = Constant.CLOCK_TIME,
                ContentBody = string.Empty
            };
            using (response = Connect ().PutObject (putObject)) {

            }
            response.Dispose ();
            GenericDelete (Constant.CLOCK_TIME);

        }

        void CreateFolderInTrash (RepositoryItem item)
        {
            CreateFolder (item.Name, item.TrashRelativePath);
        }


        #endregion
    }
}

