using System;

using System.IO;
using System.Linq;
using System.Collections.Generic;
using GreenQloud.Synchrony;
using GreenQloud.Repository;
using GreenQloud.Util;
using System.Threading;

namespace GreenQloud.Synchrony
{
    public class RemoteSynchronizer : Synchronizer
    {
        private static RemoteSynchronizer instance;

        public ChangesCapturedByWatcher<StorageQloudObject> PendingChanges;
        OSXFileSystemWatcher watcher = null;
        int controller = 0;

        protected RemoteSynchronizer ()
        {
            PendingChanges = new ChangesCapturedByWatcher<StorageQloudObject> ();
            watcher = new OSXFileSystemWatcher ();
            watcher.Changed += delegate(string path) {
                StorageQloudObject changedFile = new StorageQloudObject (path);
                if(BacklogSynchronizer.GetInstance().Status != SyncStatus.IDLE){
                    if(BacklogSynchronizer.GetInstance().ChangesInLastSync.Any(c=>c.File.FullLocalName == path))
                        return;
                }
                PendingChanges.Add (changedFile);
                if (PendingChanges.Contains(changedFile)){
                    RemoteSynchronizer.GetInstance().PendingChanges.Add(changedFile);
                    if(Directory.Exists(path)){
                        foreach (string pathFile in Directory.GetFiles(path)){
                            RemoteSynchronizer.GetInstance().PendingChanges.Add(new StorageQloudObject(pathFile));   
                        }
                    }               
                }
               Synchronize();                
            };
       }

        public static RemoteSynchronizer GetInstance ()
        {
            if (instance == null) {
                instance = new RemoteSynchronizer ();
            }
            return instance;
        }

        public override void Start ()
        {
            try {      
                Logger.LogInfo ("RemoteSynchronizer","Start RemoteSynchronizer");
            } catch (Exception e) {
                Logger.LogInfo("UploadController", e);
            }
        }

        public override void Pause ()
        {
            try {                            
                Logger.LogInfo ("RemoteSynchronizer", "Pause RemoteSynchronizer");
            } catch (Exception e) {
                Logger.LogInfo("UploadController", e);
            }
        }

        public override void Stop ()
        {
            Stoped = true;
        }

        bool Stoped {
            get;
            set;
        }

        public override void Synchronize ()
        {

            if (BacklogSynchronizer.GetInstance ().Status != SyncStatus.IDLE) {
                return;
            }

            ++controller;
            if (Status == SyncStatus.IDLE && controller == 1) {
                
                int index = 0;
                while (PendingChanges.Count != 0) {
                    if (index >= PendingChanges.Count)
                        break;
                    StorageQloudObject pendingFile = PendingChanges[index];

                    if (RemoteSynchronizer.GetInstance().Synchronize(pendingFile))
                    {   
                        Logger.LogInfo ("RemoteSynchronizer", "Removing file from Pending changes");
                        PendingChanges.Remove (pendingFile); 
                        index = 0;
                    }
                    else 
                        index++;
                }
                Status = SyncStatus.IDLE;
                controller = 0;

            }            
        }

        public bool Synchronize (StorageQloudObject file)
        {
            try{        

                if(file.IsIgnoreFile){
                    Status = SyncStatus.IDLE;
                    return true;
                }
                Logger.LogInfo("RemoteSynchronizer", "Init Synchronization: "+file.FullLocalName);
                Status = SyncStatus.VERIFING;

                if (System.IO.File.Exists (file.FullLocalName)) {

                    if (LocalSynchronizer.GetInstance().ChangesInLastSync.Any(c=> c.File.AbsolutePath== file.AbsolutePath && c.Event==WatcherChangeTypes.Created)){
                        Logger.LogInfo("RemoteSynchronizer", file.AbsolutePath+" is in list of changes made by LocalSynchronizer. This synchronization will be ignored.");
                        Status = SyncStatus.IDLE;
                        return true;
                    }
                    if (remoteRepo.Files.Any (rf => rf.RemoteMD5Hash == file.LocalMD5Hash && rf.AbsolutePath == file.AbsolutePath)){
                        Logger.LogInfo("RemoteSynchronizer", string.Format(" The versions of {0} are equals. This synchronization will be ignored.", file.AbsolutePath));
                        Status = SyncStatus.IDLE;
                        return true;
                    }
                    //Rename and moves
                    //file already exists in remote? 

                    List<StorageQloudObject> copys = remoteRepo.GetCopys(file);
                    if (copys.Count != 0) {
                        
                        Logger.LogInfo ("Synchronizing Remote",string.Format("Rename/move: {0}",file.FullLocalName));
                        //create a new copy
                        Status = SyncStatus.UPLOADING;
                        Program.Controller.RecentsTransfers.Add (remoteRepo.Copy (copys [0], file));

                        
                        //delete copys that is not in local repo and not is a trash file
                        foreach (StorageQloudObject remote in copys) {
                            if (!System.IO.File.Exists (remote.FullLocalName) && !remote.InTrash && remote.Name!=file.Name){
                                Program.Controller.RecentsTransfers.Add (remoteRepo.MoveFileToTrash (remote));
                            }
                        }
                        //if there was a rename or move, the hash remains the same 
                        BacklogSynchronizer.GetInstance ().EditFileByHash (file);
                        ChangesInLastSync.Add (new Change (file, WatcherChangeTypes.Created));
                    } else {
                        if (LocalSynchronizer.GetInstance ().ChangesInLastSync.Any (c => c.File.AbsolutePath == file.AbsolutePath && c.Event == WatcherChangeTypes.Created)){
                            Logger.LogInfo("RemoteSynchronizer", file.AbsolutePath+" is in list of changes made by LocalSynchronizer. This synchronization will be ignored.");
                            Status = SyncStatus.IDLE;
                            return true;
                        }
                        //Create
                        if (!remoteRepo.Exists(file)) {
                            if (LocalSynchronizer.GetInstance ().ChangesInLastSync.Any (c => c.File.AbsolutePath == file.AbsolutePath && c.Event == WatcherChangeTypes.Created)){
                                Logger.LogInfo("RemoteSynchronizer", file.AbsolutePath+" is in list of changes made by LocalSynchronizer. This synchronization will be ignored.");
                                Status = SyncStatus.IDLE;
                                return true;
                            }
                            Status = SyncStatus.UPLOADING;
                            
                            Logger.LogInfo ("Synchronizing Remote",string.Format("Create: {0}",file.FullLocalName));
                            
                            Program.Controller.RecentsTransfers.Add (remoteRepo.Upload (file)); 
                            ChangesInLastSync.Add (new Change (file, WatcherChangeTypes.Created));
                            BacklogSynchronizer.GetInstance ().AddFile (file);
                            LocalRepo.Files.Add (file);
                        }
                        
                        //Update
                        else if (remoteRepo.Files.Any (rf => rf.AbsolutePath == file.AbsolutePath && rf.RemoteMD5Hash != file.LocalMD5Hash)) { 
                            Status = SyncStatus.UPLOADING;
                            Logger.LogInfo ("Synchronizing Remote",string.Format("Update: {0}",file.FullLocalName));
                            remoteRepo.MoveFileToTrash (file);
                            Program.Controller.RecentsTransfers.Add (remoteRepo.Upload (file));
                            //hash changes then must be by name
                            BacklogSynchronizer.GetInstance ().EditFileByName (file);
                            ChangesInLastSync.Add (new Change (file, WatcherChangeTypes.Changed));
                            if (!LocalRepo.Files.Any (lf=> lf.FullLocalName == file.FullLocalName))
                            {
                                Logger.LogInfo ("Debug - Inconsistence", string.Format("File {0} is updated and not exists in list of files in local repo", file.AbsolutePath));
                                LocalRepo.Files.Add(file);
                            }
                        }
                    }
                    //Create folder
                } else if (Directory.Exists (file.FullLocalName)) {
                    if (!remoteRepo.Folders.Any (remo => remo.AbsolutePath.Contains (file.AbsolutePath))){
                            Logger.LogInfo ("Synchronizing Remote",string.Format("Create: {0}",file.FullLocalName));
                            Status = SyncStatus.UPLOADING;
                            Program.Controller.RecentsTransfers.Add (remoteRepo.CreateFolder (file));
                            if(Directory.Exists(file.FullLocalName)){
                                string [] files = Directory.GetFiles(file.FullLocalName);
                                string [] folders = Directory.GetDirectories (file.FullLocalName);
                                foreach (string f in files)
                                    Synchronize(new StorageQloudObject(f));
                                foreach (string d in folders)
                                    Synchronize (new StorageQloudObject(d));
                        
                        }
                    }
                 }//Deletes
                else{                   
                    if (LocalSynchronizer.GetInstance().ChangesInLastSync.Any(c=> c.File.AbsolutePath==file.AbsolutePath && c.Event == WatcherChangeTypes.Deleted))
                    {
                        Logger.LogInfo("RemoteSynchronizer", file.AbsolutePath+" is in list of changes made by LocalSynchronizer. This synchronization will be ignored.");
                        Status = SyncStatus.IDLE;
                        return true;
                    }
                    Logger.LogInfo ("Synchronizing Remote",string.Format("Delete: {0}",file.FullLocalName));
                    Status = SyncStatus.UPLOADING;
                    if (remoteRepo.ExistsFolder (file)){
                        if(DeleteFolder(file))
                            Program.Controller.RecentsTransfers.Add (new TransferResponse(file, GreenQloud.TransferType.REMOVE));
                    }
                    else{
                        DeleteFile (file);                            
                    }
                }
                remoteRepo.UpdateStorageQloud();
                Status = SyncStatus.IDLE;
               
            } catch (DisconnectionException) {
                Status = SyncStatus.IDLE;
                Program.Controller.HandleDisconnection();
                return false;
            }catch (Exception e){
                Logger.LogInfo ("UploadSynchronizer", e.Message+"\n"+e.StackTrace);
                Status = SyncStatus.IDLE;
                return false;
            }


            return true;
        }

        public bool DeleteFolder (StorageQloudObject folder)
        {

            try {
                List<StorageQloudObject> filesInFolder = remoteRepo.Files.Where (remo => remo.AbsolutePath.StartsWith (folder.AbsolutePath)).ToList<StorageQloudObject> ();
                filesInFolder.AddRange (remoteRepo.Folders.Where(remo => remo.AbsolutePath.StartsWith (folder.AbsolutePath)).ToList<StorageQloudObject> ());
                if (filesInFolder.Count >= 1) {
                    foreach (StorageQloudObject r in filesInFolder) {
                        if (folder.IsIgnoreFile)
                            continue;
                        if (r.AbsolutePath != folder.AbsolutePath)
                            DeleteFile(r);
                    }
                }
                Program.Controller.RecentsTransfers.Add (remoteRepo.MoveFolderToTrash (folder));
                if (LocalRepo.Files.Any(lf=> lf.AbsolutePath == folder.AbsolutePath+"/" || lf.AbsolutePath == folder.AbsolutePath)){
                    StorageQloudObject local = LocalRepo.Files.First(rf=> rf.AbsolutePath == folder.AbsolutePath+"/" || rf.AbsolutePath == folder.AbsolutePath);
                    LocalRepo.Files.Remove(local);
                }
                ChangesInLastSync.Add (new Change (folder, WatcherChangeTypes.Deleted));
                BacklogSynchronizer.GetInstance().RemoveFileByAbsolutePath(folder);
            } catch {
                return false;
            }
            return true;
        }

        void DeleteFile (StorageQloudObject file)
        {
            if (remoteFiles.Any (rf=>rf.FullLocalName == file.FullLocalName)){
                Program.Controller.RecentsTransfers.Add (remoteRepo.MoveFileToTrash (file));
                ChangesInLastSync.Add (new Change (file, WatcherChangeTypes.Deleted));
                BacklogSynchronizer.GetInstance ().RemoveFileByAbsolutePath (file);
                if ( LocalRepo.Files.Any (f => f.FullLocalName == file.FullLocalName)){
                    StorageQloudObject deleted = LocalRepo.Files.First (f => f.FullLocalName == file.FullLocalName);   
                    LocalRepo.Files.Remove (deleted);
                } 
                else
                    Logger.LogInfo ("Debug - Inconsistence", string.Format("File {0} should be in list of local files.", file.AbsolutePath));
            }               
            else
                Logger.LogInfo ("RemoteSynchronizer", string.Format("File {0} not exists in StorageQloud and not been deleted.", file.AbsolutePath));
        }

        public class ChangesCapturedByWatcher <File> : List<StorageQloudObject>
        {
            public event EventHandler OnAdd;
            
            public new void Add (StorageQloudObject item)
            {
                if (null != OnAdd) {
                    OnAdd (this, null);                
                }
                AddToList(item);
            }

            void AddToList (StorageQloudObject item)
            {
                try {
                    if (item.FullLocalName == RuntimeSettings.HomePath)
                        return;
                    if (item.IsIgnoreFile)
                        return;
                    if (!this.Any (f => f.FullLocalName == item.FullLocalName || item.FullLocalName.Contains (f.FullLocalName + "."))) {
                        base.Add (item);
                    }
                } catch (System.InvalidOperationException) {
                    AddToList(item);
                }
            }
        } 
    }
}

