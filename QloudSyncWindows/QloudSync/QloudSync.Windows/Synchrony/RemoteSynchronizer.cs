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
        FileSystemWatcher watcher;
        int controller = 0;
        Thread threadSynchronize;

        protected RemoteSynchronizer ()
        {

            PendingChanges = new ChangesCapturedByWatcher<StorageQloudObject> ();
            threadSynchronize = new Thread (Synchronize);
            watcher = new FileSystemWatcher ();
            watcher.Path = RuntimeSettings.HomePath;
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);           
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents= true;
            

        
       }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            
            StorageQloudObject changedFile = new StorageQloudObject(e.FullPath);
            if (BacklogSynchronizer.GetInstance().Status != SyncStatus.IDLE)
            {
                if (BacklogSynchronizer.GetInstance().ChangesInLastSync.Any(c => c.File.FullLocalName == e.FullPath))
                    return;
            }
            PendingChanges.Add(changedFile);
            if (PendingChanges.Contains(changedFile))
            {
                RemoteSynchronizer.GetInstance().PendingFiles.Add(changedFile);
                if (Directory.Exists(e.FullPath))
                {
                    foreach (string pathFile in Directory.GetFiles(e.FullPath))
                    {
                        RemoteSynchronizer.GetInstance().PendingFiles.Add(new StorageQloudObject(pathFile));
                    }
                }
            }
            if (threadSynchronize.ThreadState == ThreadState.Stopped)
                threadSynchronize.Join();
            if (threadSynchronize.ThreadState != ThreadState.Running)
            {
                threadSynchronize = new Thread(Synchronize);
                threadSynchronize.Start();
            }
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

        }

        public override void Synchronize ()
        {
            if(BacklogSynchronizer.GetInstance().Status != SyncStatus.IDLE)
                return;
            ++controller;
            if (Status == SyncStatus.IDLE && controller == 1) {
                
                int index = 0;
                while (PendingChanges.Count != 0) {
                    if (index >= PendingChanges.Count)
                        break;
                    StorageQloudObject pendingFile = PendingChanges[index];
                    
                    if (RemoteSynchronizer.GetInstance().Synchronize(pendingFile))
                    {   
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

                if(file.IsIgnoreFile)
                    return true;
                Logger.LogInfo("RemoteSynchronizer", "Init Synchronization: "+file.FullLocalName);
                PendingFiles.Remove(file);
                Status = SyncStatus.VERIFING;

                if (System.IO.File.Exists (file.FullLocalName)) {

                    if (LocalSynchronizer.GetInstance().ChangesInLastSync.Any(c=> c.File.AbsolutePath== file.AbsolutePath && c.Event==WatcherChangeTypes.Created)){
                        Logger.LogInfo("RemoteSynchronizer", file.AbsolutePath+" is in list of changes made by LocalSynchronizer. This synchronization will be ignored.");
                        return true;
                    }
                    if (remoteRepo.Files.Any (rf => rf.RemoteMD5Hash == file.LocalMD5Hash && rf.AbsolutePath == file.AbsolutePath)){
                        Logger.LogInfo("RemoteSynchronizer", string.Format(" The versions of {0} are equals. This synchronization will be ignored.", file.AbsolutePath));
                        return true;
                    }
                    //Rename and moves
                    //file already exists in remote? 

                    List<StorageQloudObject> copys = remoteRepo.GetCopys(file);
                    if (copys.Count != 0) {
                        
                        Logger.LogInfo ("Synchronizing Remote",string.Format("Rename/move: {0}",file.FullLocalName));
                        //create a new copy
                        Status = SyncStatus.UPLOADING;
                        remoteRepo.Copy (copys [0], file);
                        
                        //delete copys that is not in local repo and not is a trash file
                        foreach (StorageQloudObject remote in copys) {
                            if (!System.IO.File.Exists (remote.FullLocalName) && !remote.InTrash && remote.Name!=file.Name){
                                remoteRepo.MoveFileToTrash (remote);
                            }
                        }
                        //if there was a rename or move, the hash remains the same 
                        BacklogSynchronizer.GetInstance ().EditFileByHash (file);
                        ChangesInLastSync.Add (new Change (file, WatcherChangeTypes.Created));
                    } else {
                        //Update
                        if (LocalSynchronizer.GetInstance ().ChangesInLastSync.Any (c => c.File.AbsolutePath == file.AbsolutePath && c.Event == WatcherChangeTypes.Created)){
                            Logger.LogInfo("RemoteSynchronizer", file.AbsolutePath+" is in list of changes made by LocalSynchronizer. This synchronization will be ignored.");
                            return true;
                        }
                    
                        if (remoteRepo.Files.Any (rf => rf.AbsolutePath == file.AbsolutePath && rf.RemoteMD5Hash != file.LocalMD5Hash)) { 
                            Status = SyncStatus.UPLOADING;
                            Logger.LogInfo ("Synchronizing Remote",string.Format("Update: {0}",file.FullLocalName));
                            remoteRepo.MoveFileToTrash (file);
                            remoteRepo.Upload (file);
                            //hash changes then must be by name
                            BacklogSynchronizer.GetInstance ().EditFileByName (file);
                            ChangesInLastSync.Add (new Change (file, WatcherChangeTypes.Changed));
                        }
                        //Create
                        else if (!remoteRepo.Exists(file)) {
                            if (LocalSynchronizer.GetInstance ().ChangesInLastSync.Any (c => c.File.AbsolutePath == file.AbsolutePath && c.Event == WatcherChangeTypes.Created)){
                                Logger.LogInfo("RemoteSynchronizer", file.AbsolutePath+" is in list of changes made by LocalSynchronizer. This synchronization will be ignored.");
                                return true;
                            }
                            Status = SyncStatus.UPLOADING;
                            Logger.LogInfo ("Synchronizing Remote",string.Format("Create: {0}",file.FullLocalName));

                            remoteRepo.Upload (file); 
                            ChangesInLastSync.Add (new Change (file, WatcherChangeTypes.Created));
                            BacklogSynchronizer.GetInstance ().AddFile (file);
                        }
                    }
                    //Create folder
                } else if (Directory.Exists (file.FullLocalName)) {
                    if (!remoteRepo.Folders.Any (remo => remo.AbsolutePath.Contains (file.AbsolutePath))){
                        if (!(file.Name=="untitled folder" || file.Name=="untitled folder/")){
                            Logger.LogInfo ("Synchronizing Remote",string.Format("Create: {0}",file.FullLocalName));
                            Status = SyncStatus.UPLOADING;
                            remoteRepo.CreateFolder (file);
                            if(Directory.Exists(file.FullLocalName)){
                                string [] files = Directory.GetFiles(file.FullLocalName);
                                string [] folders = Directory.GetDirectories (file.FullLocalName);
                                foreach (string f in files)
                                    Synchronize(new StorageQloudObject(f));
                                foreach (string d in folders)
                                    Synchronize (new StorageQloudObject(d));
                        }
                        }
                    }
                 }//Deletes
                else{                   
                    if (LocalSynchronizer.GetInstance().ChangesInLastSync.Any(c=> c.File.AbsolutePath==file.AbsolutePath && c.Event == WatcherChangeTypes.Deleted))
                    {
                        Logger.LogInfo("RemoteSynchronizer", file.AbsolutePath+" is in list of changes made by LocalSynchronizer. This synchronization will be ignored.");
                        return true;
                    }
                    Logger.LogInfo ("Synchronizing Remote",string.Format("Delete: {0}",file.FullLocalName));
                    Status = SyncStatus.UPLOADING;
                    if (remoteRepo.ExistsFolder (file)){
                        DeleteFolder(file);
                    }
                    else{
                        DeleteFile (file);
                    }
                }
                Status = SyncStatus.IDLE;
               
            } catch (Exception e) {
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
                remoteRepo.MoveFolderToTrash (folder);
                BacklogSynchronizer.GetInstance().RemoveFileByAbsolutePath(folder);
            } catch {
                return false;
            }
            return true;
        }

        void DeleteFile(StorageQloudObject file){
            remoteRepo.MoveFileToTrash (file);
            ChangesInLastSync.Add (new Change(file, WatcherChangeTypes.Deleted));
            BacklogSynchronizer.GetInstance().RemoveFileByAbsolutePath(file);
        }

        List<StorageQloudObject> pendingChanges = new List<StorageQloudObject>();
        public List<StorageQloudObject> PendingFiles {
            get {
                return pendingChanges;
            }
            set {
                this.pendingChanges = value;
            }
        }

        public class ChangesCapturedByWatcher <File> : List<StorageQloudObject>
        {
            public event EventHandler OnAdd;
            
            public new void Add (StorageQloudObject item)
            {
                if (null != OnAdd) {
                    OnAdd (this, null);                
                }
                if (item.FullLocalName == RuntimeSettings.HomePath)
                    return;
                //if (Directory.Exists (item.FullLocalName))
                //  base.Add (item.ToFolder());
                //else
                if (!this.Any (f => f.FullLocalName == item.FullLocalName || item.FullLocalName.Contains (f.FullLocalName + "."))) {
                    base.Add (item);
                }
            }
        } 
    }
}

