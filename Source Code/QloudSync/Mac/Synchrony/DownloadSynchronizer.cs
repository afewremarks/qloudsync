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
        private List<GreenQloud.Repository.LocalFile> deletedFiles; 
        private List<GreenQloud.Repository.Folder> deletedFolders;
        private List<GreenQloud.Repository.RemoteFile> createdFiles;
        private List<GreenQloud.Repository.RemoteFile> updatedFiles;
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
                    if (!remoteFile.IsIgnoreFile){
//                        Console.WriteLine (remoteFile.AbsolutePath+" "+remoteFile.AsS3Object.Size);
                        //if(remoteFile.AsS3Object.Size<1000000)
                        remoteRepo.Download (remoteFile);
                    }
                }
                BacklogSynchronizer.GetInstance().AddFile(remoteFile);
            }
            LastSyncTime = DateTime.Now;
            Done = true;
        }


        protected void DescoveryRemoteChanges ()
        {
            deletedFiles = new List<GreenQloud.Repository.LocalFile> ();
            deletedFolders = new List<GreenQloud.Repository.Folder>();
            createdFiles = new List<GreenQloud.Repository.RemoteFile> ();
            updatedFiles = new List<GreenQloud.Repository.RemoteFile> ();

            foreach (GreenQloud.Repository.LocalFile localFile in LocalRepo.Files) {
                if (UploadController.GetInstance ().PendingChanges.Any (f => f.AbsolutePath == localFile.AbsolutePath))
                    continue;

                if (!ExistsInBucket (localFile))
                    deletedFiles.Add (localFile);
            }

            foreach (GreenQloud.Repository.Folder folder in LocalRepo.Folders) {
                if (UploadController.GetInstance ().PendingChanges.Any (f => f.AbsolutePath == folder.AbsolutePath))
                    continue;
                
                if (!ExistsInBucket (folder))
                    deletedFolders.Add (folder);
            }

           // List<RemoteFile> recentFiles = remoteFiles.Where(rf=>rf.TimeOfLastChange.Subtract(LastSyncTime).TotalSeconds>0).ToList<RemoteFile>();
            foreach (RemoteFile remoteFile in remoteFiles) {
                LocalFile localFile = new LocalFile (remoteFile.FullLocalName);
                if (remoteFile.ExistsInLocalRepo){
                    localFile.SetMD5Hash();
                    if (!FilesIsSync (localFile, remoteFile) && localFile.MD5Hash!=string.Empty) {                   
                        updatedFiles.Add (remoteFile);
                    }
                }
                else  createdFiles.Add(remoteFile);
            }
        }

        protected void SynchronizeDeletes ()
        {
            foreach (GreenQloud.Repository.LocalFile localFile in deletedFiles) {
                Logger.LogInfo ("Synchronizing Local", string.Format("Updating deleted changes | {0}", localFile.FullLocalName));

                string tempPath = Path.Combine (RuntimeSettings.ConfigPath, "Temp", localFile.RelativePath);
                if (!Directory.Exists (tempPath))
                    Directory.CreateDirectory(tempPath);
                tempPath = Path.Combine (tempPath, localFile.Name);
                System.IO.File.Move (localFile.FullLocalName, tempPath);
                ChangesInLastSync.Add (new Change(localFile, WatcherChangeTypes.Deleted));

                localFile.FullLocalName = tempPath;
            }

            foreach (Folder folder in deletedFolders)
                SyncRemoveFolder (folder);


            foreach (LocalFile fileToDelete in deletedFiles) {
                // Optimization: not needs download file (identify move/rename event), make a local copy 

                fileToDelete.SetMD5Hash();

                if (createdFiles.Any (c=> c.MD5Hash == fileToDelete.MD5Hash))
                {
                    GreenQloud.Repository.RemoteFile copy = createdFiles.First (c=> c.MD5Hash == fileToDelete.MD5Hash);

                    IOHelper.CreateParentFolders(copy.FullLocalName);
                    System.IO.File.Move (fileToDelete.FullLocalName, copy.FullLocalName);
                    ChangesInLastSync.Add (new Change(copy, WatcherChangeTypes.Created));
                    createdFiles.Remove (copy);
                    LocalRepo.Files.Add(copy.ToLocalFile());
                }

                if (!ExistsVersion (fileToDelete)){
                    List<RemoteFile> copys = remoteRepo.AllFiles.Where (rf => rf.MD5Hash == fileToDelete.MD5Hash && rf.AbsolutePath != fileToDelete.AbsolutePath).ToList<RemoteFile> ();
                    if (copys.Count != 0) 
                        remoteRepo.CopyToTrashFolder (copys [0], fileToDelete);
                    else
                        remoteRepo.SendToTrash (fileToDelete);
                }   
                if(System.IO.File.Exists (fileToDelete.FullLocalName)){
                    System.IO.File.Delete(fileToDelete.FullLocalName);                    
                    LocalRepo.Files.Remove (fileToDelete);
                    Logger.LogInfo("Synchronizer","Local file "+fileToDelete.Name+" was deleted.");
                }
            }
        }


        protected void SynchronizeCreates ()
        {
            foreach (RemoteFile remoteFile in createdFiles) {
                if (remoteFile.IsAFolder)
                {
                    Logger.LogInfo ("Synchronizing Local", string.Format("Updating creating changes | {0}", remoteFile.FullLocalName));
                    Folder folder = remoteFile.ToFolder();
                    if (!Directory.Exists(folder.FullLocalName)) {
                        folder.Create ();                
                        ChangesInLastSync.Add (new Change(folder, WatcherChangeTypes.Created));
                        BacklogSynchronizer.GetInstance().AddFile (folder);
                    }
                }
                else
                {
                    if (UploadController.GetInstance ().PendingChanges.Any (f => f.AbsolutePath == remoteFile.AbsolutePath) 
                        || UploadSynchronizer.GetInstance ().ChangesInLastSync.Any (c => c.Event == WatcherChangeTypes.Deleted && c.File.AbsolutePath == remoteFile.AbsolutePath))
                        return;
                    //create
                    remoteFile.TimeOfLastChange = DateTime.Now;
                    remoteRepo.Download (remoteFile);
                    LocalRepo.Files.Add (remoteFile.ToLocalFile());
                    ChangesInLastSync.Add (new Change (remoteFile, WatcherChangeTypes.Created));
                    BacklogSynchronizer.GetInstance ().AddFile (remoteFile);
                }
            }
        }

        protected void SynchronizeUpdates ()
        {
            foreach (RemoteFile remoteFile in updatedFiles) {
                
                Logger.LogInfo ("Synchronizing Local", string.Format("Updating changes | {0}", remoteFile.FullLocalName));
                MoveToTrashFolder (remoteFile.ToLocalFile());
                // baixa arquivo remoto
                remoteFile.TimeOfLastChange = DateTime.Now;
                remoteRepo.Download (remoteFile);
                ChangesInLastSync.Add (new Change(remoteFile, WatcherChangeTypes.Changed));
                BacklogSynchronizer.GetInstance().EditFileByName (remoteFile);
            }
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
            DescoveryRemoteChanges();
            SynchronizeDeletes();
            SynchronizeCreates();
            SynchronizeUpdates();
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
	            foreach (GreenQloud.Repository.File localFile in LocalRepo.Files)
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