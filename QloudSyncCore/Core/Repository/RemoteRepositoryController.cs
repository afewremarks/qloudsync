using System;
using System.Collections.Generic;
using System.Linq;
using GreenQloud.Util;
using GreenQloud.Model;
using GreenQloud.Repository;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.IO;
using System.Text;
using QloudSync.Repository;
using System.Threading;
using LitS3;
using System.Web.Util;
using GreenQloud.Synchrony;

namespace GreenQloud.Repository
{
    public class RemoteRepositoryController : AbstractController, IRemoteRepositoryController
    {
        private PhysicalRepositoryController physicalController;
        private S3Connection connection;
        public RemoteRepositoryController (LocalRepository repo) : base(repo){
            HttpEncoder.Current = HttpEncoder.Default;
            connection = new S3Connection ();
            physicalController = new PhysicalRepositoryController (repo);
        }
       
        public List<GreenQloud.Model.RepositoryItem> GetItems(string prefix) {
            return GetInstancesOfItems(GetS3Objects(prefix).Where(i =>!Key(i).StartsWith(".trash")).ToList());
        }
        public List<GreenQloud.Model.RepositoryItem> RootFolders {
            get {
                return GetInstancesOfItems(GetRootFoldersS3Objects().Where(i => !Key(i).StartsWith(".trash")).ToList());
            }
        }

        public bool Exists (RepositoryItem item)
        {
            try{
                S3Service service = connection.Connect ();

                var request = new LitS3.GetObjectRequest(service, RuntimeSettings.DefaultBucketName, item.Key, true);
                using (GetObjectResponse response = request.GetResponse()) { 
                    //todo...
                }
            } catch (WebException webx) {
                if (((HttpWebResponse)webx.Response).StatusCode == HttpStatusCode.NotFound)
                    return false;
                else
                    throw webx;
            }

            return true;
        }

        #region Manage Itens
        public void Download (RepositoryItem item, bool recursive = false)
        {
            physicalController.CreatePath(item.LocalFolderPath);
            if (item.IsFolder) {
                physicalController.CreateFolder(item);
                if (recursive)
                    DownloadEntry(connection.Connect ().ListObjects (RuntimeSettings.DefaultBucketName, item.Key), item);
            } else {
                GenericDownload (item.Key, item.LocalAbsolutePath);
            }
        }

        private void DownloadEntry(IEnumerable<ListEntry> entries, RepositoryItem father){
            foreach(ListEntry entry in entries ){
                if(Key(entry) != string.Empty){
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
                GenericCreateFolder (item.Key);
                UploadEntry(connection.Connect ().ListObjects (RuntimeSettings.DefaultBucketName, item.Key), item);
            }else{
                GenericUpload (item.Key,  item.LocalAbsolutePath);
            }
        }

        private void UploadEntry(IEnumerable<ListEntry> entries, RepositoryItem father){
            foreach(ListEntry entry in entries ){
                if(Key(entry) != string.Empty){
                    RepositoryItem item = CreateObjectInstance (entry);
                    if(item.Key != father.Key){
                        Upload (item);
                    }
                }
            }
        }

        public void Move (RepositoryItem item)
        {
            if(item.IsFolder){
                GenericCopy (item.Key, item.ResultItem.Key);
                MoveEntry(connection.Connect ().ListObjects (RuntimeSettings.DefaultBucketName, item.Key), item);
            }else{
                GenericCopy (item.Key, item.ResultItem.Key);
            }

            Delete (item);
        }

        private void MoveEntry(IEnumerable<ListEntry> entries, RepositoryItem father){
            foreach(ListEntry entry in entries ){
                if(Key(entry) != string.Empty){
                    RepositoryItem item = CreateObjectInstance (entry);
                    item.BuildResultItem (Path.Combine(father.ResultItem.Key, item.Name));
                    if(item.Key != father.Key){
                        Move (item);
                    }
                }
            }
        }

        public void Copy (RepositoryItem item)
        {
            if(item.IsFolder){
                GenericCopy (item.Key, item.ResultItem.Key);
                CopyEntry(connection.Connect ().ListObjects (RuntimeSettings.DefaultBucketName, item.Key), item);
            }else{
                GenericCopy (item.Key, item.ResultItem.Key);
            }
        }

        private void CopyEntry(IEnumerable<ListEntry> entries, RepositoryItem father){
            foreach(ListEntry entry in entries ){
                if(Key(entry) != string.Empty){
                    RepositoryItem item = CreateObjectInstance (entry);
                    if(item.Key != father.Key){
                        Copy (item);
                    }
                }
            }
        }

        public void Delete (RepositoryItem item)
        {
            GenericDelete (item.Key, item.IsFolder);
        }

        public string RemoteETAG (RepositoryItem item)
        {
            GetObjectResponse meta = GetMetadata (item.Key);
            if(meta == null){
                Logger.LogInfo("ERROR", "ETAG from " + item.Key + " cannot be calculated. Metadata not found!");
                return "";
            }
            return meta.ETag.Replace("\"", "");
        }

        public long GetContentLength(string key)
        {
            return GetMetadata(key).ContentLength;
        }

        public GetObjectResponse GetMetadata (string key, bool recoveryIfFolderBroken = false)
        {
            S3Service service = connection.Connect ();
            try{
                var request = new LitS3.GetObjectRequest(service, RuntimeSettings.DefaultBucketName, key, true);
                using (GetObjectResponse response = request.GetResponse()){
                    return response;
                }
            } catch (WebException webx) {
                if (webx != null && ((HttpWebResponse)webx.Response) != null && ((HttpWebResponse)webx.Response).StatusCode == HttpStatusCode.NotFound)
                {
                    try {
                        //METADATA NOT FOUND, recover it (if is folder)!
                        if(recoveryIfFolderBroken){
                            if(key.EndsWith("/")){
                                GenericCreateFolder(key);
                                Logger.LogInfo("INFO", "METADATA FOR "+ key +" NOT FOUND, trying to recover it!");
                                GetObjectRequest request2 = new LitS3.GetObjectRequest(service, RuntimeSettings.DefaultBucketName, key, true);
                                using (GetObjectResponse response2 = request2.GetResponse()){
                                    Logger.LogInfo("INFO", "METADATA FOR "+ key +" RECOVERED, folder created!");
                                    return response2;
                                }
                            }
                        }
                        Logger.LogInfo("INFO", "METADATA FOR "+ key +" CANNOT BE RECOVERED!");
                        return null;
                    } catch {
                        Logger.LogInfo("INFO", "METADATA FOR "+ key +" CANNOT BE RECOVERED!");
                        return null;
                    }
                } else {
                    throw webx;
                }
            }
        }

        #endregion
     

        #region Generic
        private void GenericCopy (string sourceKey, string destinationKey)
        {
            connection.Connect ().CopyObject (RuntimeSettings.DefaultBucketName, sourceKey, destinationKey);
        }

        private void GenericDelete (string key, bool keyAsPrefix = false)
        {
            if (keyAsPrefix) {
                connection.Connect ().ForEachObject (RuntimeSettings.DefaultBucketName, key, DeleteEntry);
                GenericDelete (key);
            } else {
                connection.Connect ().DeleteObject (RuntimeSettings.DefaultBucketName, key);
            }
        }
        private void DeleteEntry(ListEntry entry){
            if(Key(entry) != string.Empty){
                if (entry is CommonPrefix) {
                    GenericDelete (Key(entry), true);
                } else {
                    connection.Connect ().DeleteObject (RuntimeSettings.DefaultBucketName, Key(entry));
                }
            }
        }


        private void GenericDownload (string key, string localAbsolutePath)
        {
            BlockWatcher (localAbsolutePath);
            connection.Connect().GetObject(RuntimeSettings.DefaultBucketName, key, localAbsolutePath);
            UnblockWatcher (localAbsolutePath);
        }

        private void GenericUpload (string key, string filepath)
        {
            connection.Connect().AddObject(filepath, RuntimeSettings.DefaultBucketName, key);
        }

        private void GenericCreateFolder (string key)
        {
            string objectContents = string.Empty;
            connection.Connect ().AddObject (RuntimeSettings.DefaultBucketName, key, 0, stream =>
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
        public IEnumerable<ListEntry> GetRootFoldersS3Objects ()
        {
            HttpEncoder.Current = HttpEncoder.Default;
            List<ListEntry> entries = new List<ListEntry> ();
            entries = connection.Connect ().ListAllObjects (RuntimeSettings.DefaultBucketName).Where(i => Key(i).EndsWith("/")).ToList();
            return entries;
        }

        private IEnumerable<ListEntry> GetS3Objects (string prefix)
        {
            HttpEncoder.Current = HttpEncoder.Default;
            IEnumerable<ListEntry> subEntries = connection.Connect ().ListAllObjects (RuntimeSettings.DefaultBucketName, prefix).ToList();
            List<ListEntry> entries = new List<ListEntry> ();
            foreach (ListEntry entry in subEntries) {
                if (Key (entry) != string.Empty && Key (entry) != prefix) {
                    entries.Add (entry);
                }
            }
            return entries;
        }

        protected List<RepositoryItem> GetInstancesOfItems (IEnumerable<ListEntry> s3items)
        {
            List <RepositoryItem> remoteItems = new List <RepositoryItem> ();
            foreach (ListEntry s3item in s3items) {
                RepositoryItem item = CreateObjectInstance (s3item);
                if(item != null){
                    remoteItems.Add (item);  
                }
            }
            return remoteItems;
        }

        public RepositoryItem CreateObjectInstance (ListEntry s3item)
        {
            string key = Key (s3item);
            if (key != string.Empty) {
                RepositoryItem item = RepositoryItem.CreateInstance (repo, s3item);
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

