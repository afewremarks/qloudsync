using System;
using System.Linq;

using System.IO;
using System.Collections.Generic;
using GreenQloud.Repository;
using GreenQloud.Util;


namespace GreenQloud.Synchrony
{
    public class DownloadSynchronizer :  Synchrony.Synchronizer
    {
        private static DownloadSynchronizer instance;
        private List<GreenQloud.Repository.RemoteFile> PendingFiles;
 
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
            Done = false;
            
            remoteRepo.Connection.TransferSize = 0;

                List<RemoteFile> remoteFiles = remoteRepo.Files;
                CalculateDownloadSize(remoteFiles);
                foreach (RemoteFile remoteFile in remoteFiles) {
                    if(remoteFile.IsAFolder)
                        Directory.CreateDirectory (remoteFile.FullLocalName);
                    else
                    {
                        if (!remoteFile.IsIgnoreFile)
                            remoteRepo.Download (remoteFile);

                    }

                    BacklogSynchronizer.GetInstance().AddFile(remoteFile);
                }
            Done = true;
        }

        public void Synchronize ()
        {            
            Done = false;
            remoteRepo.Connection.TransferSize = 0;
            Logger.LogInfo("Synchronizer", "Trying download files from Storage.");
            DateTime initTime = DateTime.Now;
            ChangesInLastSync = new List<GreenQloud.Repository.Change>();
            SyncSize = 0;
            Initialize();
            PendingFiles = RemoteChanges;
            SyncRemoteUpdates ();
            SyncClear ();
            ShowDoneMessage ("Download");
            LastSyncTime = initTime;
            Done = true;
        }

        void CalculateDownloadSize (List<RemoteFile> remoteFiles)
        {
            foreach (RemoteFile remoteFile in remoteFiles) {
                if (!remoteFile.IsIgnoreFile)
                    Size += remoteFile.AsS3Object.Size;
            }
        }

        public List<RemoteFile> RemoteChanges {
            get {
                TimeSpan diffClocks = remoteRepo.DiffClocks;
                DateTime referencialClock = LastSyncTime.Subtract (diffClocks);
                return remoteRepo.Files.Where (rf => Convert.ToDateTime (rf.AsS3Object.LastModified).Subtract (referencialClock).TotalSeconds > 0).ToList<RemoteFile>();
            }
        }
        
        public bool HasRemoteChanges {
            get {
                return RemoteChanges.Count != 0;
            }
        }


        private void SyncRemoteUpdates ()
        {
            foreach (RemoteFile remoteFile in PendingFiles)
            {
               if(!remoteFile.IsAFolder)
                    SyncFile (remoteFile);
                else 
                    SyncFolder (remoteFile.ToFolder());
            }
        }
        
        
        private void SyncFile (RemoteFile remoteFile)
        {
            LocalFile localFile = new LocalFile (remoteFile.FullLocalName);
            
            if (localFile.IsFileLocked)
                return;            
            
            if (remoteFile.InTrash || remoteFile.IsIgnoreFile)
                return;
            
            Logger.LogInfo ("Synchronizer","Synchronizing: "+remoteFile.Name);
            
            if (localFile.ExistsInLocalRepo)
            {
                //update
                if (!FilesIsSync (localFile, remoteFile))
                {
                    // nao estando sincronizado
                    // faz upload do arquivo local para o trash com referencia ao arquivo remoto
                    //localFile.RecentVersion = remoteFile;
                    MoveToTrashFolder (localFile);
                    // baixa arquivo remoto
                    remoteFile.TimeOfLastChange = DateTime.Now;
 				remoteRepo.Download (remoteFile);
                    LocalRepo.Files.Add (new LocalFile(remoteFile.AbsolutePath));
                    ChangesInLastSync.Add (new Change(remoteFile, WatcherChangeTypes.Changed));
                    BacklogSynchronizer.GetInstance().EditFileByName (remoteFile);
                }
            }
            else
            {
                if (UploadController.GetInstance().PendingChanges.Any (f=> f.AbsolutePath==remoteFile.AbsolutePath) 
                    || UploadSynchronizer.GetInstance().ChangesInLastSync.Any(c=> c.Event == WatcherChangeTypes.Deleted && c.File.AbsolutePath==remoteFile.AbsolutePath))
                    return;
                //create
                remoteFile.TimeOfLastChange = DateTime.Now;
                remoteRepo.Download (remoteFile);
                LocalRepo.Files.Add (new LocalFile(remoteFile.AbsolutePath));
                ChangesInLastSync.Add (new Change(remoteFile, WatcherChangeTypes.Created));
                BacklogSynchronizer.GetInstance().AddFile(remoteFile);
            }
            localFiles = LocalRepo.Files;
        }
        
        private void SyncFolder (Folder folder)
        {           
            if (!Directory.Exists(folder.FullLocalName)) {
                folder.Create ();                
                ChangesInLastSync.Add (new Change(folder, WatcherChangeTypes.Created));
                BacklogSynchronizer.GetInstance().AddFile (folder);
            }           
        }
        
        
        private void SyncClear ()
        {
			try{
	            foreach (GreenQloud.Repository.File localFile in localFiles)
	            {
                    if(localFile.Deleted)
						continue;
                  
                    if (UploadController.GetInstance().PendingChanges.Any(f=> f.AbsolutePath==localFile.AbsolutePath))
                        continue;

	                if (localFile is Folder)
	                    SyncRemoveFolder ((Folder) localFile);
	                else
	                    SyncRemoveFile ((LocalFile) localFile);
	            }
			}catch (System.InvalidOperationException)
			{
				Logger.LogInfo ("Debug", "Collection was modified; SyncClear");
				SyncClear();
			}
        }
        
        private void SyncRemoveFile (LocalFile localFile)
        {            
            if (localFile.IsIgnoreFile)
                return;

            if (!ExistsInBucket (localFile))
            {

                if (!ExistsVersion (localFile)){
                    MoveToTrashFolder (localFile);
				}
                //else copia
                else if(System.IO.File.Exists (localFile.FullLocalName)){

                    System.IO.File.Delete(localFile.FullLocalName);                    
                    ChangesInLastSync.Add (new Change(localFile, WatcherChangeTypes.Deleted));
                }

                BacklogSynchronizer.GetInstance().RemoveFileByHash (localFile);
            }

        }
        
        private void SyncRemoveFolder (Folder folder)
		{
            //TODO fazer uma rotina de exclusao de pastas
			DirectoryInfo d = new DirectoryInfo (folder.FullLocalName);

			if (!ExistsInBucket (folder) && d.Exists) {
				ExcludeFolder (d);
                
                BacklogSynchronizer.GetInstance().RemoveFileByHash (folder);
			}
        }

		void ExcludeFolder (DirectoryInfo folder)
		{
			foreach (FileInfo f in folder.GetFiles()){
                f.Delete();
                ChangesInLastSync.Add (new Change (new LocalFile(f.FullName), WatcherChangeTypes.Deleted));
			}
			foreach (DirectoryInfo id in folder.GetDirectories()){
				ExcludeFolder (id);
			}
			folder.Delete ();
            ChangesInLastSync.Add (new Change (new LocalFile(folder.FullName), WatcherChangeTypes.Deleted));
		}
        
        private void MoveToTrashFolder (LocalFile localFile)
        {
            Logger.LogInfo ("Synchronizer", "Versioning: " + localFile.Name);
            
            if (remoteRepo.SendToTrash (localFile)) 
            {
                ChangesInLastSync.Add(new Change(localFile, WatcherChangeTypes.Deleted));
				System.IO.File.Delete(localFile.FullLocalName);    
                Logger.LogInfo("Synchronizer","Local file "+localFile.Name+" was deleted.");
            }
        }

   }
}
