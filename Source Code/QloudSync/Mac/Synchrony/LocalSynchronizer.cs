using System;
using System.Linq;

using System.IO;
using System.Collections.Generic;
using GreenQloud.Repository;
using GreenQloud.Util;
using System.Collections;


namespace GreenQloud.Synchrony
{
    public class DownloadSynchronizer :  Synchrony.Synchronizer
    {
        private static DownloadSynchronizer instance;
        private List<StorageQloudObject> PendingFiles;
        public static bool Initialized;
        
        
        private DownloadSynchronizer ()
        { 
            Initialized = false;
            LastSyncTime = DateTime.Now;
        }
        
        public static DownloadSynchronizer GetInstance()
        {
            if (instance == null)
                instance = new DownloadSynchronizer();            
            
            return instance;
        }

        public void FullLoad ()
        {
            remoteRepo.Connection.TransferSize = 0;
            List<StorageQloudObject> remoteFiles = remoteRepo.Files;
            List<StorageQloudObject> remoteFolders = remoteRepo.Folders;
            CalculateDownloadSize(remoteFiles);

            foreach (StorageQloudObject remoteFile in remoteFiles) {

                if (remoteFile.IsIgnoreFile)
                    continue;
               
                remoteRepo.Download (remoteFile);            
                BacklogSynchronizer.GetInstance().AddFile(remoteFile);
            }

            foreach(StorageQloudObject folder in remoteFolders){
                if(!Directory.Exists (folder.FullLocalName)){
                    Directory.CreateDirectory(folder.FullLocalName);
                    BacklogSynchronizer.GetInstance().AddFile(folder);
                }
            }

            LastSyncTime = DateTime.Now;
            Done = true;
        }

        public void Synchronize ()
        {
            Done = false;    
            remoteRepo.Connection.TransferSize = 0;
            DateTime initTime = DateTime.Now;
            ChangesInLastSync = new List<GreenQloud.Repository.Change>();
            SyncSize = 0;  
            PendingFiles = RemoteChanges;
            SynchronizeUpdates ();
            LastSyncTime = initTime;
            Done = true;
        }

        protected void SynchronizeUpdates ()
        {

            List<StorageQloudObject> deletedFiles = new List<StorageQloudObject> ();
            
            foreach (StorageQloudObject localFile in LocalRepo.Files) {
                bool deleted = false;
                if (localFile.IsAFolder) {
                    deleted = !remoteRepo.FolderExistsInBucket (localFile);
                } else {
                    deleted = !remoteRepo.ExistsInBucket (localFile);
                }
                
                if (deleted)
                    deletedFiles.Add (localFile);
            }
            
            foreach (StorageQloudObject deletedFile in deletedFiles) { 
                if (deletedFile.IsIgnoreFile)
                    continue;
                if (RemoteSynchronizer.GetInstance().PendingFiles.Any (f=> f.AbsolutePath == deletedFile.AbsolutePath))
                    continue;
                if (RemoteSynchronizer.GetInstance().ChangesInLastSync.Any (c=> c.File.AbsolutePath == deletedFile.AbsolutePath && c.Event == WatcherChangeTypes.Created))
                    continue;
                
                Logger.LogInfo ("LocalSynchronizer", string.Format ("\"{0}\" was deleted in StorageQloud", deletedFile.FullLocalName));
                Logger.LogInfo ("LocalSynchronizer", string.Format ("Deleting \"{0}\" in local repo", deletedFile.FullLocalName));
                LocalRepo.Delete (deletedFile);
                ChangesInLastSync.Add (new Change (deletedFile, WatcherChangeTypes.Deleted));
            }

            if (PendingFiles.Count != 0) {
                foreach (StorageQloudObject remoteFile in PendingFiles) {
                    if (RemoteSynchronizer.GetInstance ().ChangesInLastSync.Any (c => c.File.AbsolutePath == remoteFile.AbsolutePath && c.Event == WatcherChangeTypes.Created)) {
                        RemoteSynchronizer.GetInstance ().ChangesInLastSync.Remove (new Change (remoteFile, WatcherChangeTypes.Created));
                        continue;
                    }
                    if (RemoteSynchronizer.GetInstance ().PendingFiles.Any (f => f.AbsolutePath == remoteFile.AbsolutePath))
                        continue;
                    bool create = !LocalRepo.Exists (remoteFile);
                    if (!create) {
                        if (!remoteFile.IsSync) {
                            Logger.LogInfo ("LocalSynchronizer", string.Format ("File \"{0}\" is outdated and will be dowloaded", remoteFile.FullLocalName));
                            remoteRepo.SendLocalVersionToTrash (remoteFile);
                            remoteRepo.Download (remoteFile);
                            ChangesInLastSync.Add (new Change (remoteFile, WatcherChangeTypes.Created));
                            BacklogSynchronizer.GetInstance ().EditFileByName (remoteFile);
                        }
                    } else {
                        Logger.LogInfo ("LocalSynchronizer", string.Format ("File \"{0}\" not exists in local repo and will be downloaded", remoteFile.FullLocalName));
                        remoteRepo.Download (remoteFile);
                        BacklogSynchronizer.GetInstance ().AddFile (remoteFile);
                        LocalRepo.Files.Add (remoteFile);
                    }
                }
            }

            foreach (StorageQloudObject folder in remoteRepo.Folders) {
                LocalRepo.CreateFolder(folder);
                ChangesInLastSync.Add(new Change(folder, WatcherChangeTypes.Created));
            }
        }


        void CalculateDownloadSize (List<StorageQloudObject> remoteFiles)
        {
            foreach (StorageQloudObject remoteFile in remoteFiles) {
                if (!remoteFile.IsIgnoreFile)
                    Size += remoteFile.AsS3Object.Size;
            }
        }

        public List<StorageQloudObject> RemoteChanges {
            get {

                TimeSpan diffClocks = remoteRepo.DiffClocks;
                DateTime referencialClock = LastSyncTime.Subtract (diffClocks);               
                List<StorageQloudObject> list = remoteRepo.Files.Where (rf => Convert.ToDateTime (rf.AsS3Object.LastModified).Subtract (referencialClock).TotalSeconds > 0).ToList<StorageQloudObject>();

                return list;
            }
        }
        
        public bool HasRemoteChanges {
            get {
                return RemoteChanges.Count != 0;
            }
        }
   }
}