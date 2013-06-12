using System;
using System.Collections.Generic;
using System.Linq;
using GreenQloud.Util;
using GreenQloud.Model;
using GreenQloud.Repository.Local;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.IO;
using System.Text;
using QloudSync.Repository;
using System.Threading;
using LitS3;

namespace GreenQloud.Repository
{
    public class RemoteRepositoryController : AbstractController, IRemoteRepositoryController
    {
        private StorageQloudPhysicalRepositoryController physicalController;
        private S3Connection connection;
        private string DefaultBucketName;
        public RemoteRepositoryController (){
            connection = new S3Connection ();
            physicalController = new StorageQloudPhysicalRepositoryController ();
            DefaultBucketName = "gsn";
        }

        public List<GreenQloud.Model.RepositoryItem> Items {
            get {
                return GetInstancesOfItems (GetS3Objects().Where(i => !i.Name.StartsWith(".")).ToList());
            }
        }

        public List<GreenQloud.Model.RepositoryItem> AllItems {
            get {
                return GetInstancesOfItems (GetS3Objects());
            }
        }

        public List<GreenQloud.Model.RepositoryItem> TrashItems {
            get {
                return GetInstancesOfItems (GetS3Objects().Where(i => i.Name.StartsWith(Constant.TRASH)).ToList());
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
                DownloadEntry(connection.Connect ().ListObjects (DefaultBucketName, item.Key), item);
            } else {
                GenericDownload (item.Key, item.LocalAbsolutePath);
            }
        }

        private void DownloadEntry(IEnumerable<ListEntry> entries, RepositoryItem father){
            foreach(ListEntry entry in entries ){
                if(entry.Name != string.Empty){
                    RepositoryItem item = CreateObjectInstance (entry);
                    if(item.Key != father.Key){
                        Download (item);
                    }
                }
            }
        }

        public void Upload (RepositoryItem item)
        {
            if(item.IsFolder){
                UploadFolder (item);
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
            GenericDelete (item.Key, item.IsFolder);
        }

        public void UploadFolder (RepositoryItem item)
        {
            UploadFolder (item.Key);
        }

        public string RemoteETAG (RepositoryItem item)
        {
            return GetMetadata(item.Key).ETag;
        }

        public GetObjectResponse GetMetadata (string key)
        {
            S3Service service = connection.Connect ();
            var metadataOnly = true;
            var request = new LitS3.GetObjectRequest(service, DefaultBucketName, key, metadataOnly);
            using (LitS3.GetObjectResponse response = request.GetResponse())
                return response; 
        }
        #endregion
     

        #region Generic
        private void GenericCopy (string sourceKey, string destinationKey)
        {
            connection.Connect ().CopyObject (DefaultBucketName, sourceKey, destinationKey);
        }

        private void GenericDelete (string key, bool keyAsPrefix = false)
        {
            if (keyAsPrefix) {
                connection.Connect ().ForEachObject (DefaultBucketName, key, DeleteEntry);
                GenericDelete (key);
            } else {
                connection.Connect ().DeleteObject (DefaultBucketName, key);
            }
        }
        private void DeleteEntry(ListEntry entry){
            if(entry.Name != string.Empty){
                connection.Connect ().DeleteObject (DefaultBucketName, Key(entry));
            }
        }


        private void GenericDownload (string key, string localAbsolutePath)
        {
            BlockWatcher (localAbsolutePath);
            connection.Connect().GetObject(DefaultBucketName, key, localAbsolutePath);
            UnblockWatcher (localAbsolutePath);
        }

        private void GenericUpload (string key, string filepath)
        {
            connection.Connect().AddObject(filepath, DefaultBucketName, key);
        }

        private void UploadFolder (string key)
        {
            string objectContents = string.Empty;
            connection.Connect ().AddObject (DefaultBucketName, key, 0, stream =>
                                                {
                                                    var writer = new StreamWriter(stream, Encoding.ASCII);
                                                    writer.Write(objectContents);
                                                    writer.Flush();
                                                });
        }

        private void DeleteFolder (string key){
            GenericDelete (key, true);
        }

        #endregion


        #region Handle S3Objects
        public IEnumerable<ListEntry> GetS3Objects ()
        {
            return connection.Connect ().ListAllObjects (DefaultBucketName);
        }

        protected List<RepositoryItem> GetInstancesOfItems (IEnumerable<ListEntry> s3items)
        {
            List <RepositoryItem> remoteItems = new List <RepositoryItem> ();
            foreach (ListEntry s3item in s3items) {
                if (!s3item.Name.Contains(Constant.TRASH))
                {    
                    remoteItems.Add ( CreateObjectInstance (s3item));
                    //add folders that have items to persist too                   
                }
            }
            return remoteItems;
        }

        public RepositoryItem CreateObjectInstance (ListEntry s3item)
        {
            if (s3item.Name != string.Empty) {
                LocalRepository repo;
                string key = Key (s3item);
                repo = new Persistence.SQLite.SQLiteRepositoryDAO ().FindOrCreateByRootName (RuntimeSettings.HomePath);
                RepositoryItem item = RepositoryItem.CreateInstance (repo, GetMetadata (key).ContentLength == 0, s3item);
                return item;
            }
            return null;
        }

        private string Key(ListEntry s3item){
            string key = string.Empty;
            if (s3item is CommonPrefix) {
                key = ((CommonPrefix)s3item).Prefix;
            } else {
                key = ((ObjectEntry)s3item).Key;
            }
            return key;
        }
        #endregion

    }
}

