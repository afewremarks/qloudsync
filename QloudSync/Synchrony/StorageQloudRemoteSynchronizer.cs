//using System;
//
//using System.IO;
//using System.Linq;
//using System.Collections.Generic;
//using GreenQloud.Synchrony;
//using GreenQloud.Repository;
//using GreenQloud.Util;
//using System.Threading;
//using GreenQloud.Model;
//
//namespace GreenQloud.Synchrony
//{
//    public class StorageQloudRemoteSynchronizer : RemoteSynchronizer
//    {
//        private static StorageQloudRemoteSynchronizer instance;
//
//        public ChangesCapturedByWatcher<RepositoryItem> PendingChanges;
//        OSXFileSystemWatcher watcher = null;
//        int controller = 0;
//
//        protected StorageQloudRemoteSynchronizer ()
//        {
//            PendingChanges = new ChangesCapturedByWatcher<RepositoryItem> ();
//            watcher = new OSXFileSystemWatcher ();
//            watcher.Changed += delegate(string path) {
////                RepositoryItem changedFile = new RepositoryItem (path);
////                if(BacklogSynchronizer.GetInstance().Status != SyncStatus.IDLE){
////                    if(BacklogSynchronizer.GetInstance().ChangesInLastSync.Any(c=>c.File.FullLocalName == path))
////                        return;
////                }
////                PendingChanges.Add (changedFile);
////                if (PendingChanges.Contains(changedFile)){
////                    RemoteSynchronizer.GetInstance().PendingChanges.Add(changedFile);
////                    if(Directory.Exists(path)){
////                        foreach (string pathFile in Directory.GetFiles(path)){
////                            RemoteSynchronizer.GetInstance().PendingChanges.Add(new RepositoryItem(pathFile));   
////                        }
////                    }               
////                }
//               Synchronize();                
//            };
//       }
//
//        public static StorageQloudRemoteSynchronizer GetInstance ()
//        {
//            if (instance == null) {
//                instance = new StorageQloudRemoteSynchronizer ();
//            }
//            return instance;
//        }
//
//        public override void Start ()
//        {
//            try {      
//                Logger.LogInfo ("RemoteSynchronizer","Start RemoteSynchronizer");
//            } catch (Exception e) {
//                Logger.LogInfo("UploadController", e);
//            }
//        }
//
//        public override void Pause ()
//        {
//            try {                            
//                Logger.LogInfo ("RemoteSynchronizer", "Pause RemoteSynchronizer");
//            } catch (Exception e) {
//                Logger.LogInfo("UploadController", e);
//            }
//        }
//
//        public override void Stop ()
//        {
//            Stoped = true;
//        }
//
//        bool Stoped {
//            get;
//            set;
//        }
//
//        public override void Synchronize ()
//        {
//
////            if (BacklogSynchronizer.GetInstance ().Status != SyncStatus.IDLE) {
////                return;
////            }
//
//            ++controller;
//            if (Status == SyncStatus.IDLE && controller == 1) {
//                
//                int index = 0;
//                while (PendingChanges.Count != 0) {
//                    if (index >= PendingChanges.Count)
//                        break;
//                    RepositoryItem pendingFile = PendingChanges[index];
//
//                    if (StorageQloudRemoteSynchronizer.GetInstance().Synchronize(pendingFile))
//                    {   
//                        Logger.LogInfo ("RemoteSynchronizer", "Removing file from Pending changes");
//                        PendingChanges.Remove (pendingFile); 
//                        index = 0;
//                    }
//                    else 
//                        index++;
//                }
//                Status = SyncStatus.IDLE;
//                controller = 0;
//
//            }            
//        }
//
//        public bool Synchronize (RepositoryItem file)
//        {
////            try{        
////
////                if(file.IsIgnoreFile){
////                    Status = SyncStatus.IDLE;
////                    return true;
////                }
////                Logger.LogInfo("RemoteSynchronizer", "Init Synchronization: "+file.FullLocalName);
////                Status = SyncStatus.VERIFING;
////
////                if (System.IO.File.Exists (file.FullLocalName)) {
////
////                    if (LocalSynchronizer.GetInstance().ChangesInLastSync.Any(c=> c.File.AbsolutePath== file.AbsolutePath && c.Event==WatcherChangeTypes.Created)){
////                        Logger.LogInfo("RemoteSynchronizer", file.AbsolutePath+" is in list of changes made by LocalSynchronizer. This synchronization will be ignored.");
////                        Status = SyncStatus.IDLE;
////                        return true;
////                    }
////                    if (remoteRepository.Files.Any (rf => rf.RemoteMD5Hash == file.LocalMD5Hash && rf.AbsolutePath == file.AbsolutePath)){
////                        Logger.LogInfo("RemoteSynchronizer", string.Format(" The versions of {0} are equals. This synchronization will be ignored.", file.AbsolutePath));
////                        Status = SyncStatus.IDLE;
////                        return true;
////                    }
////                    //Rename and moves
////                    //file already exists in remote? 
////
////                    List<RepositoryItem> copys = remoteRepository.GetCopys(file);
////                    if (copys.Count != 0) {
////                        
////                        Logger.LogInfo ("Synchronizing Remote",string.Format("Rename/move: {0}",file.FullLocalName));
////                        //create a new copy
////                        Status = SyncStatus.UPLOADING;
////                        Program.Controller.RecentsTransfers.Add (remoteRepository.Copy (copys [0], file));
////
////                        //delete copys that is not in local repo and not is a trash file
////                        foreach (RepositoryItem remote in copys) {
////                            if (!System.IO.File.Exists (remote.FullLocalName) && !remote.InTrash && remote.Name!=file.Name){
////                                Logger.LogInfo ("Debug - Inconsistence", string.Format("File {0} exist in remote repositoy but not exists in local repository and not was handled", remote.AbsolutePath));
////                                Program.Controller.RecentsTransfers.Add (remoteRepository.MoveFileToTrash (remote));
////                                ChangesInLastSync.Add (new Change(remote, WatcherChangeTypes.Deleted));
////                                if (LocalRepo.Files.Any(lf=>lf.FullLocalName == remote.FullLocalName))
////                                {
////                                    RepositoryItem deleted = LocalRepo.Files.First (lf=>lf.FullLocalName == remote.FullLocalName);
////                                    LocalRepo.Files.Remove (deleted);
////                                }
////                                //BacklogSynchronizer.GetInstance().RemoveFileByAbsolutePath (remote);
////                            }
////                        }
////                        //the delete code of old file exclude the refference of file in backlog, being necessary to re-add 
////                        //BacklogSynchronizer.GetInstance ().AddFile (file);
////                        ChangesInLastSync.Add (new Change (file, WatcherChangeTypes.Created));
////                    } else {
////                        if (LocalSynchronizer.GetInstance ().ChangesInLastSync.Any (c => c.File.AbsolutePath == file.AbsolutePath && c.Event == WatcherChangeTypes.Created)){
////                            Logger.LogInfo("RemoteSynchronizer", file.AbsolutePath+" is in list of changes made by LocalSynchronizer. This synchronization will be ignored.");
////                            Status = SyncStatus.IDLE;
////                            return true;
////                        }
////                        //Create
////                        if (!remoteRepository.Exists(file)) {
////                            if (LocalSynchronizer.GetInstance ().ChangesInLastSync.Any (c => c.File.AbsolutePath == file.AbsolutePath && c.Event == WatcherChangeTypes.Created)){
////                                Logger.LogInfo("RemoteSynchronizer", file.AbsolutePath+" is in list of changes made by LocalSynchronizer. This synchronization will be ignored.");
////                                Status = SyncStatus.IDLE;
////                                return true;
////                            }
////                            Status = SyncStatus.UPLOADING;
////                            
////                            Logger.LogInfo ("Synchronizing Remote",string.Format("Create: {0}",file.FullLocalName));
////                            
////                            Program.Controller.RecentsTransfers.Add (remoteRepository.Upload (file)); 
////                            ChangesInLastSync.Add (new Change (file, WatcherChangeTypes.Created));
////                            //BacklogSynchronizer.GetInstance ().AddFile (file);
////                            LocalRepo.Files.Add (file);
////                        }
////                        
////                        //Update
////                        else if (remoteRepository.Files.Any (rf => rf.AbsolutePath == file.AbsolutePath && rf.RemoteMD5Hash != file.LocalMD5Hash)) { 
////                            Status = SyncStatus.UPLOADING;
////                            Logger.LogInfo ("Synchronizing Remote",string.Format("Update: {0}",file.FullLocalName));
////                            remoteRepository.MoveFileToTrash (file);
////                            Program.Controller.RecentsTransfers.Add (remoteRepository.Upload (file));
////                            //hash changes then must be by name
////                            //BacklogSynchronizer.GetInstance ().EditFileByName (file);
////                            ChangesInLastSync.Add (new Change (file, WatcherChangeTypes.Changed));
////                            if (!LocalRepo.Files.Any (lf=> lf.FullLocalName == file.FullLocalName))
////                            {
////                                Logger.LogInfo ("Debug - Inconsistence", string.Format("File {0} is updated and not exists in list of files in local repo", file.AbsolutePath));
////                                LocalRepo.Files.Add(file);
////                            }
////                        }
////                    }
////                    //Create folder
////                } else if (Directory.Exists (file.FullLocalName)) {
////                    if (!remoteRepository.Folders.Any (remo => remo.AbsolutePath.Contains (file.AbsolutePath))){
////                            Logger.LogInfo ("Synchronizing Remote",string.Format("Create: {0}",file.FullLocalName));
////                            Status = SyncStatus.UPLOADING;
////                            Program.Controller.RecentsTransfers.Add (remoteRepository.CreateFolder (file));
////                            if(Directory.Exists(file.FullLocalName)){
////                                string [] files = Directory.GetFiles(file.FullLocalName);
////                                string [] folders = Directory.GetDirectories (file.FullLocalName);
////                                foreach (string f in files)
////                                    Synchronize(new RepositoryItem(f));
////                                foreach (string d in folders)
////                                    Synchronize (new RepositoryItem(d));
////                        
////                        }
////                    }
////                 }//Deletes
////                else{                   
////                    if (LocalSynchronizer.GetInstance().ChangesInLastSync.Any(c=> c.File.AbsolutePath==file.AbsolutePath && c.Event == WatcherChangeTypes.Deleted))
////                    {
////                        Logger.LogInfo("RemoteSynchronizer", file.AbsolutePath+" is in list of changes made by LocalSynchronizer. This synchronization will be ignored.");
////                        Status = SyncStatus.IDLE;
////                        return true;
////                    }
////                    Logger.LogInfo ("Synchronizing Remote",string.Format("Delete: {0}",file.FullLocalName));
////                    Status = SyncStatus.UPLOADING;
////                    if (remoteRepository.ExistsFolder (file)){
////                        if(DeleteFolder(file))
////                            Program.Controller.RecentsTransfers.Add (new Transfer(file, GreenQloud.TransferType.REMOVE));
////                    }
////                    else{
////                        DeleteFile (file);                            
////                    }
////                }
////                remoteRepository.UpdateStorageQloud();
////                Status = SyncStatus.IDLE;
////               
////            } catch (DisconnectionException) {
////                Status = SyncStatus.IDLE;
////                Program.Controller.HandleDisconnection();
////                return false;
////            }catch (Exception e){
////                Logger.LogInfo ("UploadSynchronizer", e.Message+"\n"+e.StackTrace);
////                Status = SyncStatus.IDLE;
////                return false;
////            }
//
//
//            return true;
//        }
//
//        public bool DeleteFolder (RepositoryItem folder)
//        {
//
////            try {
////                List<RepositoryItem> filesInFolder = remoteRepository.Files.Where (remo => remo.AbsolutePath.StartsWith (folder.AbsolutePath)).ToList<RepositoryItem> ();
////                filesInFolder.AddRange (remoteRepository.Folders.Where(remo => remo.AbsolutePath.StartsWith (folder.AbsolutePath)).ToList<RepositoryItem> ());
////                if (filesInFolder.Count >= 1) {
////                    foreach (RepositoryItem r in filesInFolder) {
////                        if (folder.IsIgnoreFile)
////                            continue;
////                        if (r.AbsolutePath != folder.AbsolutePath)
////                            DeleteFile(r);
////                    }
////                }
////                Program.Controller.RecentsTransfers.Add (remoteRepository.MoveFolderToTrash (folder));
////                if (LocalRepo.Files.Any(lf=> lf.AbsolutePath == folder.AbsolutePath+"/" || lf.AbsolutePath == folder.AbsolutePath)){
////                    RepositoryItem local = LocalRepo.Files.First(rf=> rf.AbsolutePath == folder.AbsolutePath+"/" || rf.AbsolutePath == folder.AbsolutePath);
////                    LocalRepo.Files.Remove(local);
////                }
////                ChangesInLastSync.Add (new Change (folder, WatcherChangeTypes.Deleted));
////                //BacklogSynchronizer.GetInstance().RemoveFileByAbsolutePath(folder);
////            } catch {
////                return false;
////            }
//            return true;
//        }
//
//        void DeleteFile (RepositoryItem file)
//        {
////            if (remoteFiles.Any (rf=>rf.FullLocalName == file.FullLocalName)){
////                Program.Controller.RecentsTransfers.Add (remoteRepository.MoveFileToTrash (file));
////                ChangesInLastSync.Add (new Change (file, WatcherChangeTypes.Deleted));
////                //BacklogSynchronizer.GetInstance ().RemoveFileByAbsolutePath (file);
////                if ( LocalRepo.Files.Any (f => f.FullLocalName == file.FullLocalName)){
////                    RepositoryItem deleted = LocalRepo.Files.First (f => f.FullLocalName == file.FullLocalName);   
////                    LocalRepo.Files.Remove (deleted);
////                } 
////                else
////                    Logger.LogInfo ("Debug - Inconsistence", string.Format("File {0} should be in list of local files.", file.AbsolutePath));
////            }               
////            else
////                Logger.LogInfo ("RemoteSynchronizer", string.Format("File {0} not exists in StorageQloud and not been deleted.", file.AbsolutePath));
//        }
//
//        public class ChangesCapturedByWatcher <File> : List<RepositoryItem>
//        {
//            public event EventHandler OnAdd;
//            
//            public new void Add (RepositoryItem item)
//            {
//                if (null != OnAdd) {
//                    OnAdd (this, null);                
//                }
//                AddToList(item);
//            }
//
//            void AddToList (RepositoryItem item)
//            {
//                try {
//                    if (item.FullLocalName == RuntimeSettings.HomePath)
//                        return;
//                    if (item.IsIgnoreFile)
//                        return;
//                    if (!this.Any (f => f.FullLocalName == item.FullLocalName || item.FullLocalName.Contains (f.FullLocalName + "."))) {
//                        base.Add (item);
//                    }
//                } catch (System.InvalidOperationException) {
//                    AddToList(item);
//                }
//            }
//        } 
//    }
//}
//
