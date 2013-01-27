using System;
using System.Linq;

using System.IO;
using System.Collections.Generic;
using QloudSync.Repository;
using QloudSync.Util;


namespace  QloudSync.Synchrony
{
    public class DownloadSynchronizer :  Synchrony.Synchronizer
    {
        private static DownloadSynchronizer instance;
        private List<QloudSync.Repository.RemoteFile> PendingFiles;
 
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
            remoteRepo.Connection.TransferSize = 0;
            Logger.LogInfo("Synchronizer", "Trying download files from Storage.");
            DateTime initTime = DateTime.Now;
            remoteRepo.FilesChanged = new List<QloudSync.Repository.File>();
            SyncSize = 0;
            Initialize();
            PendingFiles = RemoteChanges;
            SyncRemoteUpdates ();
            SyncClear ();
            ShowDoneMessage ("Download");
            LastSyncTime = initTime;
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
             //   if (!UploadController.GetInstance().PendingChanges.Where (c => c.File.FullLocalName == remoteFile.FullLocalName && c.Event == WatcherChangeTypes.Deleted).Any()){
                    if(!remoteFile.IsAFolder)
                        SyncFile (remoteFile);
                    else 
                        SyncFolder (new Folder (remoteFile.FullLocalName));
               // }
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
                if ( !FilesIsSync (localFile, remoteFile))
                {
                    Console.WriteLine ("Not sync "+localFile.MD5Hash+" "+remoteFile.MD5Hash);
                    // nao estando sincronizado
                    // faz upload do arquivo local para o trash com referencia ao arquivo remoto
                    //localFile.RecentVersion = remoteFile;
                    MoveToTrashFolder (localFile);
                    // baixa arquivo remoto
                    //Changes.Add (connection.Download (remoteFile));
                    AddDownloadFile (remoteFile);
					remoteRepo.Download (remoteFile);
                    BacklogSynchronizer.GetInstance().EditFileByName (remoteFile);
                }
            }
            else
            {
                //create
				AddDownloadFile(remoteFile);
                remoteRepo.Download (remoteFile);
                BacklogSynchronizer.GetInstance().AddFile(remoteFile);
                countOperation++;
            }   
        }
        
        private void SyncFolder (Folder folder)
        {           
            if (!new DirectoryInfo (folder.FullLocalName).Exists) {
                folder.Create ();
                BacklogSynchronizer.GetInstance().AddFile (folder);
            }
            //countOperation++;
        }
        
        
        private void SyncClear ()
        {
			try{
	            foreach (QloudSync.Repository.File localFile in localFiles)
	            {
					if(localFile.Deleted)
						continue;
                    if (UploadController.GetInstance().PendingChanges.Where(c => c.File.AbsolutePath == localFile.AbsolutePath).Any())
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
	        //se arquivo local nao existe no repositorio remoto

            if (!ExistsInBucket (localFile))
            {
				if (!ExistsVersion (localFile)){
	                MoveToTrashFolder (localFile);
				}
                else
                {
                    FileInfo f = new FileInfo(localFile.FullLocalName);
                    if (f.Exists)
                        f.Delete();
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
				remoteRepo.FilesChanged.Add(new RemoteFile(f.FullName));
				f.Delete();
			}
			foreach (DirectoryInfo id in folder.GetDirectories()){
				ExcludeFolder (id);
			}
			remoteRepo.FilesChanged.Add (new LocalFile(folder.FullName));
			folder.Delete ();
		}
        
        private void MoveToTrashFolder (LocalFile localFile)
        {
            Logger.LogInfo ("Synchronizer", "Versioning: " + localFile.Name);
            
            if (remoteRepo.SendToTrash (localFile)) 
            {
				remoteRepo.FilesChanged.Add (localFile);
                new FileInfo(localFile.FullLocalName).Delete ();    
                Logger.LogInfo("Synchronizer","Local file "+localFile.Name+" was deleted.");
            }
        }

        void AddDownloadFile (RemoteFile remoteFile)
        {
            remoteFile.TimeOfLastChange = DateTime.Now;
            remoteRepo.FilesChanged.Add (remoteFile);
        }
    }
}