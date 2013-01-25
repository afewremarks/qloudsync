using System;

using System.IO;
using System.Linq;
using System.Collections.Generic;
using QloudSync.Synchrony;
using QloudSync.Repository;
using QloudSync.Util;


namespace  QloudSync.Synchrony
{
	public class UploadSynchronizer : Synchronizer
	{
		private static UploadSynchronizer instance;

        private List<Change> pendingChanges;
        public List<Change> PendingChanges {
            set {
                this.pendingChanges = value;
            }
            get {
                return pendingChanges;
            }
        }

		public static UploadSynchronizer GetInstance ()
		{
			if (instance == null) {
				instance = new UploadSynchronizer ();
			}
			return instance;
		}
		
		public override void Synchronize ()
		{
            Synchronized = false;
			Logger.LogInfo ("Synchronizer", "Trying upload files to Storage.");
			DateTime initTime = DateTime.Now;
			if (Initialize ()) {
				SyncFiles ();
				ShowDoneMessage ("Upload"); 
			}
			Repo.LastSyncTime = initTime;
            Synchronized = true;

		}

		private void SyncFiles ()
        {

                int attempt = 0;
                int index = 0;
                Console.WriteLine (DateTime.Now.ToUniversalTime () + " - Pending Changes (" + PendingChanges.Count + ") ");

                while (PendingChanges.Count != 0) {
                   
                    if (index >= PendingChanges.Count)
                        break;
                    
                    
                    Change change = PendingChanges [index];
                    if(change != null)
                    {
                        bool operationSuccessfully = false;

                        switch (change.Event) {
                        case WatcherChangeTypes.Deleted:
                            operationSuccessfully = DeleteFile (change.File);
                            break;
                        case WatcherChangeTypes.Renamed:
                            operationSuccessfully = RenameFile (change.File);
                            break;
                        case WatcherChangeTypes.Created:
                                operationSuccessfully = CreateFile (change.File);
                            break;
                        case WatcherChangeTypes.Changed:
                            if (HasChangeMustRecent (change))
                                operationSuccessfully = true;
                            else
                                operationSuccessfully = UpdateFile (change.File);
                            break;
                        }

                        if (operationSuccessfully) {
                            PendingChanges.Remove (change);
                            attempt = 0;
                            index = 0;
                        } else {
                            Console.WriteLine ("Fail "+change.Event+" to "+change.File.FullLocalName);
                            if (attempt >= 3) {
                                index++;
                                attempt = 0;
                            } else
                                attempt ++;
                        }
                    }
                }

		}




		bool HasChangeMustRecent (Change change)
        {
            try {
                foreach (Change otherchange in PendingChanges) {
                    if (otherchange.Event == WatcherChangeTypes.Renamed && otherchange.File.OldVersion != null) {
                        if (otherchange.File.OldVersion.FullLocalName == change.File.FullLocalName) {
                            otherchange.Event = change.Event;
                            return true;
                        }
                    }
                }
            } catch (Exception e){
                Logger.LogInfo("Sync", e);
            }
			return false;
		}

		bool CreateFile (QloudSync.Repository.File file)
		{
            
			try {
				if (file.IsAFolder)
					remoteRepo.CreateFolder (new Folder (file.FullLocalName));
				else
                    remoteRepo.Upload (file);
				return true;
			} catch (Exception e) {
				Console.WriteLine (e.Message);
				Console.WriteLine (e.StackTrace);
				return false;
			}
		}

		
		bool UpdateFile (QloudSync.Repository.File file)
		{
   
			try {
				RemoteFile remoteFile = (RemoteFile)LocalFile.Get 
				(file, remoteFiles.Cast <QloudSync.Repository.File> ().ToList ()); 
				if (!FilesIsSync ((LocalFile)file, remoteFile)) {
					// indica que a versao mais atual eh a local
					remoteFile.RecentVersion = file;
					// envia para o trash a versao desatualizada
                    remoteRepo.MoveToTrash (remoteFile);
					// faz o upload da versao recente
                    remoteRepo.Upload (file);
				}
				return true;
			} catch {
				return false;
			}		
		}

		bool DeleteFile (QloudSync.Repository.File file)
		{

			try {
                List<RemoteFile> filesInFolder = remoteFiles.Where (remo => remo.AbsolutePath.Contains(file.AbsolutePath+Path.PathSeparator)).ToList<RemoteFile>();

                if (filesInFolder.Count >= 1){
                    file = new Folder (file.FullLocalName+Path.PathSeparator);
                    foreach (RemoteFile r in filesInFolder)
                    {
                        if(file.IsIgnoreFile)
                            continue;
                        if (r.AbsolutePath != file.AbsolutePath)
                            remoteRepo.MoveToTrash (r);

                    }
                }

               remoteRepo.MoveToTrash (file);
			} catch {
				return false;
			}
			return true;
		}

		bool RenameFile (QloudSync.Repository.File file)
		{
			if (remoteFiles.Where (rf => rf.AbsolutePath == file.AbsolutePath).Any ()) {
				try {
					if (file.IsAFolder)
						SynchronizeUploadRenameFolder ((Folder)file);
					else
						SynchronizeUploadRenameFile ((LocalFile)file);
				} catch {
					return false;
				}
				return true;
			} else 
				return CreateFile (file);

		}

		
		private void SynchronizeUploadRenameFile (LocalFile localFile)
		{
			// renomeia o arquivo remoto
            remoteRepo.Move (localFile.OldVersion, localFile);
		}
		
		private void SynchronizeUploadRenameFolder (Folder folder)
		{

			// nao precisa fazer upload, pois vai mover apenas remotamente (os arquivos atuais ja estao la)
			FileInfo[] filesInFolder = new DirectoryInfo(folder.FullLocalName).GetFiles();
			if (filesInFolder.Length == 0)
                remoteRepo.Move (folder.OldVersion, folder);
			else {
				foreach (FileInfo file in filesInFolder)
				{
					LocalFile newFile = new LocalFile (file.FullName);
					LocalFile oldFile = new LocalFile 
						(file.FullName.Replace (folder.FullLocalName, folder.OldVersion.FullLocalName));
                    remoteRepo.Move (oldFile, newFile);
				}
			}
		}


		/*public void GeneralSynchronize ()
		{
			Logger.LogInfo ("Synchronizer", "Trying upload files to Storage.");
			DateTime initTime = DateTime.Now;
			if (Initialize ()) {
				GeneralSyncFiles ();
				GeneralSyncFolders ();
				ShowDoneMessage ("Upload"); 
			}
			LastSyncTime = initTime;
		}
		
		
		private void GeneralSyncFiles ()
		{
			//para cada arquivo
			foreach (File  SQObj in localFiles)
			{
				
				//verifica se nao esta ignorado
				if ( SQObj is LocalFile)
					//sincroniza arquivo
					GeneralSyncFile ((LocalFile) SQObj);
				
			}
		}
		
		private void GeneralSyncFile (LocalFile localFile)
		{
			if (localFile.IsIgnoreFile)
				return;
			Logger.LogInfo ("Synchronizer","Synchronizing: "+localFile.Name);
			try
			{
				// pega o arquivo remoto correspondente
				RemoteFile remoteFile = (RemoteFile) LocalFile.Get 
					(localFile, remoteFiles.Cast <File>().ToList()); 

    
				// se nao existe esse arquivo no bucket
				if (remoteFile == null) {
					// faz o upload

                    RemoteRepo.Upload (localFile);
					countOperation ++;
				}
				else if ( !FilesIsSync (localFile, remoteFile) )
				{
                    TimeSpan diffClocks = RemoteRepo.DiffClocks;
					DateTime referencialClock = new FileInfo(localFile.FullLocalName).LastWriteTime.Subtract (diffClocks);

					//local mais recente
					if (referencialClock.Subtract(Convert.ToDateTime(remoteFile.AsS3Object.LastModified)).TotalSeconds>0)
					{
						// indica que a versao mais atual eh a local
						remoteFile.RecentVersion = localFile;
						// envia para o trash a versao desatualizada
                        RemoteRepo.MoveToTrash (remoteFile);
						// faz o upload da versao recente
                        RemoteRepo.Upload (localFile);
					}else
                        RemoteRepo.Download(remoteFile);
				}

			}
			catch (Exception e ){
				Logger.LogInfo("Synchronizer","Fail to Upload: File "+localFile.Name+" is open or used by other process.\n");
                Console.WriteLine (e.GetType());
                Console.WriteLine (e.StackTrace);
			}
		}
		
		private void GeneralSyncFolders ()
		{
			// recupera todas as pastas vazias no repositorio local
			foreach (Folder folder in localEmptyFolders) 
			{
				// se nao existe a pasta no repositorio remoto 
				if (!remoteFiles.Where (r => r.AbsolutePath == folder.AbsolutePath).Any())
				{
					//cria
                    RemoteRepo.CreateFolder(folder);
					//countOperation ++;
				}
			}
		}*/		
	}
}

