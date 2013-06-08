using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.S3.Model;
using GreenQloud.Util;
using GreenQloud.Model;
using GreenQloud.Repository.Local;
using Amazon.S3;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.IO;
using System.Text;
using QloudSync.Repository;
using System.Threading;

namespace GreenQloud.Repository
{
    public class RemoteRepositoryController : IRemoteRepositoryController
    {
        private StorageQloudPhysicalRepositoryController physicalController;
        private S3Connection connection;

        public RemoteRepositoryController (){
            connection = new S3Connection ();
            physicalController = new StorageQloudPhysicalRepositoryController ();
        }

        public List<GreenQloud.Model.RepositoryItem> Items {
            get {
                return GetInstancesOfItems (GetS3Objects().Where(i => !i.Key.StartsWith(Constant.TRASH)).ToList());
            }
        }

        public List<GreenQloud.Model.RepositoryItem> AllItems {
            get {
                return GetInstancesOfItems (GetS3Objects());
            }
        }

        public List<GreenQloud.Model.RepositoryItem> TrashItems {
            get {
                return GetInstancesOfItems (GetS3Objects().Where(i => i.Key.StartsWith(Constant.TRASH)).ToList());
            }
        }

        public List<GreenQloud.Model.RepositoryItem> GetCopys (RepositoryItem item)
        {
            return AllItems.Where (rf => rf.ETag == item.LocalETag && rf.Key != item.Key).ToList<RepositoryItem> ();
        }

        public bool ExistsCopies (RepositoryItem item)
        {
            return GetCopys(item).Count > 0;
        }

        public bool Exists (RepositoryItem item)
        {
            return Items.Any (rf => rf.Key == item.Key);
        }

        #region Manage Itens
        public void Download (RepositoryItem item)
        {
            if (item.IsFolder) {
                physicalController.CreateFolder(item);
            } else {
                GenericDownload (item.Key, item.LocalAbsolutePath);
            }
        }

        public void Upload (RepositoryItem item)
        {
            if(item.IsFolder){
                CreateFolder (item);
            }else{
                GenericUpload (item.Key,  item.LocalAbsolutePath);
            }
        }

        public void Move (RepositoryItem item)
        {
            GenericCopy (item.Key, item.ResultItem.Key);
            Delete (item);
        }

        public void Copy (RepositoryItem item)
        {
            GenericCopy (item.Key, item.ResultItem.Key);
        }

        public void Delete (RepositoryItem item)
        {
            GenericDelete (item.Key);
        }

        public void CreateFolder (RepositoryItem item)
        {
            CreateFolder (item.Key);
        }

        public string RemoteETAG (RepositoryItem item)
        {
            return GetMetadata(item.Key).ETag;
        }
        #endregion
     

        #region Generic
        private GetObjectMetadataResponse GetMetadata (string key)
        {
            GetObjectMetadataRequest request = new GetObjectMetadataRequest {
                BucketName = RuntimeSettings.DefaultBucketName,
                Key = key
            };
            GetObjectMetadataResponse response;
            AmazonS3Client con;
            using (con = connection.Connect ()) {
                using (response = con.GetObjectMetadata (request)) {
                }
            }
            response.Dispose ();
            con.Dispose ();
            return response;
        }


        private void GenericCopy (string sourceKey, string destinationKey)
        {
            CopyObjectRequest request = new CopyObjectRequest (){
                DestinationBucket = RuntimeSettings.DefaultBucketName,
                DestinationKey = destinationKey,
                SourceBucket = RuntimeSettings.DefaultBucketName,
                SourceKey = sourceKey
            };
            AmazonS3Client con;
            S3Response cor;
            using (con = connection.Connect ()) {   
                using (cor = con.CopyObject (request)){
                }
            }
            cor.Dispose ();
            con.Dispose ();
        }

        public void GenericDelete (string key)
        {
            DeleteObjectRequest request = new DeleteObjectRequest (){
                BucketName = RuntimeSettings.DefaultBucketName,
                Key = key
            };
            AmazonS3Client con;
            DeleteObjectResponse response;
            using (con = connection.Connect()) {
                using (response = con.DeleteObject (request)) {
                }
            }
            response.Dispose ();
            con.Dispose ();
        }

        void GenericDownload (string key, string localAbsolutePath)
        {
            GetObjectResponse response = null;
            GetObjectRequest objectRequest = new GetObjectRequest () {
                BucketName = RuntimeSettings.DefaultBucketName,
                Key = key
            };
            AmazonS3Client con;
            using (con = connection.Connect ()) {
                using (response = con.GetObject (objectRequest)) {
                    response.WriteResponseStreamToFile (localAbsolutePath);
                }
            }
            response.Dispose ();
            con.Dispose ();
        }

        private void GenericUpload (string key, string filepath)
        {
            PutObjectRequest putObject = new PutObjectRequest () {
                BucketName = RuntimeSettings.DefaultBucketName,
                FilePath = filepath,
                Key = key, 
                Timeout = GlobalSettings.UploadTimeout
            };
            AmazonS3Client con;
            PutObjectResponse response;
            using (con = connection.Connect()){
                using(response = con.PutObject (putObject)) {
                }
            }
            response.Dispose ();
            con.Dispose ();
        }

        private void CreateFolder (string key)
        {
            PutObjectResponse response;
            PutObjectRequest putObject = new PutObjectRequest (){
                BucketName = RuntimeSettings.DefaultBucketName,
                Key = key,
                ContentBody = string.Empty
            };
            AmazonS3Client con;
            using (con = connection.Connect())
            {
                using(response = con.PutObject (putObject))
                {
                }
            }
            response.Dispose();
            con.Dispose();
        }
        #endregion


        #region Handle S3Objects
        public List<S3Object> GetS3Objects ()
        {
            ListObjectsRequest request = new ListObjectsRequest () {
                BucketName = RuntimeSettings.DefaultBucketName
            };
            List<S3Object> list;

            AmazonS3Client con;
            ListObjectsResponse response;
            using (con = connection.Connect())
            {
                using(response = con.ListObjects(request)){
                    do{
                        list = response.S3Objects;

                        // If response is truncated, set the marker to get the next 
                        // set of keys.
                        if (response.IsTruncated)
                        {                           
                            request.Marker = response.NextMarker;
                        }
                    } while (response.IsTruncated);
                } 
            }
            response.Dispose ();
            con.Dispose ();
            return list;
        }

        protected List<RepositoryItem> GetInstancesOfItems (List<S3Object> s3items)
        {
            List <RepositoryItem> remoteItems = new List <RepositoryItem> ();
            foreach (S3Object s3item in s3items) {
                if (!s3item.Key.StartsWith(Constant.TRASH))
                {    
                    remoteItems.Add ( CreateObjectInstance (s3item));
                    //add folders that have items to persist too                   
                }
            }
            return remoteItems;
        }

        public RepositoryItem CreateObjectInstance (S3Object s3item)
        {
            LocalRepository repo;
            repo = new Persistence.SQLite.SQLiteRepositoryDAO().FindOrCreateByRootName (RuntimeSettings.HomePath);
            RepositoryItem item = RepositoryItem.CreateInstance (repo, s3item);
            return item;
        }
        #endregion

    }
}

