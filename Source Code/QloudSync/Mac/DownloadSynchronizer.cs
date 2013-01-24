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
        }
        
        public static DownloadSynchronizer GetInstance()
        {
            if (instance == null)
                instance = new DownloadSynchronizer();
            
            
            return instance;
        }
        
        public override bool Synchronize ()
        {
            Synchronized = false;
            Logger.LogInfo("Synchronizer", "Trying download files from Storage.");
            DateTime initTime = DateTime.Now;
            RemoteRepo.FilesChanged = new List<QloudSync.Repository.File>();
            SyncSize = 0;
            if (Initialize ()) {
                PendingFiles = RemoteChanges;
                foreach (RemoteFile remoteFile in PendingFiles)
                {
                    if (!remoteFile.IsIgnoreFile)
                        SyncSize += remoteFile.AsS3Object.Size;
                }


                SyncRemoteUpdates ();
                SyncClear ();
                ShowDoneMessage ("Download");
                Synchronized = true;
            }
            Repo.LastSyncTime = initTime;
            return true;
        }
        
        
      /*  public bool DownloadRemoteFiles ()
        {
            Synchronized = false;
            Logger.LogInfo("Synchronizer", "Trying download files from Storage.");
            DateTime initTime = DateTime.Now;
            SyncSize = 0;
            if (Initialize ()) 
            {
                foreach (RemoteFile remoteFile in remoteFiles)
                {
                    if (!remoteFile.IsIgnoreFile)
                        SyncSize += remoteFile.AsS3Object.Size;
                }
                Initialized = true;

                foreach (RemoteFile remoteFile in remoteFiles)
                {
                    if (remoteFile.InTrash || remoteFile.IsIgnoreFile)
                        continue;

                    if(!remoteFile.IsAFolder)
                    {
                        LocalFile localFile = new LocalFile (remoteFile.FullLocalName);
                        
                        if (localFile.IsFileLocked)
                            continue;

                        Logger.LogInfo ("Synchronizer","Synchronizing: "+remoteFile.Name);
                        
                        if (!localFile.ExistsInLocalRepo)
                        {
                            // se nao existe, baixa
                            RemoteRepo.Download (remoteFile);

                            countOperation++;
                        }   
                        
                    }
                    else 
                        SyncFolder (new Folder (remoteFile.FullLocalName));
                }
                ShowDoneMessage ("Download");
            }
            LastSyncTime = initTime;
            Synchronized = true;
            return true;
        }*/
        
        private void SyncRemoteUpdates ()
        {
            foreach (RemoteFile remoteFile in PendingFiles)
            {
                if (!LocalRepo.PendingChanges.Where (c => c.File.FullLocalName == remoteFile.FullLocalName && c.Event == WatcherChangeTypes.Deleted).Any()){
                    if(!remoteFile.IsAFolder)
                        SyncFile (remoteFile);
                    else 
                        SyncFolder (new Folder (remoteFile.FullLocalName));
                }
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
					RemoteRepo.Download (remoteFile);
                }
            }
            else
            {
                // se nao existe, baixa
				AddDownloadFile(remoteFile);
                RemoteRepo.Download (remoteFile);
                
                countOperation++;
            }   
        }
        
        private void SyncFolder (Folder folder)
        {           
            if (!new DirectoryInfo (folder.FullLocalName).Exists )
                folder.Create();
            //countOperation++;
        }
        
        
        private void SyncClear ()
        {
			try{
	            foreach (QloudSync.Repository.File localFile in localFiles)
	            {
					if(localFile.Deleted)
						continue;
	                if (LocalRepo.PendingChanges.Where(c => c.File.AbsolutePath == localFile.AbsolutePath).Any())
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
            }

        }
        
        private void SyncRemoveFolder (Folder folder)
		{

			//TODO fazer uma rotina de exclusao de pastas
			DirectoryInfo d = new DirectoryInfo (folder.FullLocalName);

			if (!ExistsInBucket (folder) && d.Exists) {
				ExcludeFolder (d);
			}
        }

		void ExcludeFolder (DirectoryInfo folder)
		{
			foreach (FileInfo f in folder.GetFiles()){
				RemoteRepo.FilesChanged.Add(new RemoteFile(f.FullName));
				f.Delete();
			}
			foreach (DirectoryInfo id in folder.GetDirectories()){
				ExcludeFolder (id);
			}
			RemoteRepo.FilesChanged.Add (new LocalFile(folder.FullName));
			folder.Delete ();
		}
        
        private void MoveToTrashFolder (LocalFile localFile)
        {
            Logger.LogInfo ("Synchronizer", "Versioning: " + localFile.Name);
            
            if (RemoteRepo.SendToTrash (localFile)) 
            {
				RemoteRepo.FilesChanged.Add (localFile);
                new FileInfo(localFile.FullLocalName).Delete ();    
                Logger.LogInfo("Synchronizer","Local file "+localFile.Name+" was deleted.");
            }
        }

        void AddDownloadFile (RemoteFile remoteFile)
        {
            remoteFile.TimeOfLastChange = DateTime.Now;
            RemoteRepo.FilesChanged.Add (remoteFile);
        }
    }
}