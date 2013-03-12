using System;

using System.IO;
using System.Linq;
using System.Collections.Generic;
using GreenQloud.Synchrony;
using GreenQloud.Repository;
using GreenQloud.Util;

namespace GreenQloud.Synchrony
{
    public class RemoteSynchronizer : Synchronizer
    {
        private static RemoteSynchronizer instance;

        protected RemoteSynchronizer ()
        {

        }

        public static RemoteSynchronizer GetInstance ()
        {
            if (instance == null) {
                instance = new RemoteSynchronizer ();
            }
            return instance;
        }

        public bool Synchronize (StorageQloudObject file)
        {
            try{        
                if(file.IsIgnoreFile)
                    return true;
                PendingFiles.Remove(file);
                if (System.IO.File.Exists (file.FullLocalName)) {
                    if (DownloadSynchronizer.GetInstance().ChangesInLastSync.Any(c=> c.File.AbsolutePath== file.AbsolutePath && c.Event==WatcherChangeTypes.Created))
                        return true;
                    if (remoteRepo.Files.Any (rf => rf.RemoteMD5Hash == file.LocalMD5Hash && rf.AbsolutePath == file.AbsolutePath))
                        return true;
                    //Rename and moves
                    //file already exists in remote? 

                    List<StorageQloudObject> copys = remoteRepo.AllFiles.Where (rf => rf.RemoteMD5Hash == file.LocalMD5Hash && rf.AbsolutePath != file.AbsolutePath && !rf.Name.EndsWith("/") && rf.AsS3Object.Size>0).ToList<StorageQloudObject> ();

                    if (copys.Count != 0) {
                        
                        Logger.LogInfo ("Synchronizing Remote",string.Format("Rename/move: {0}",file.FullLocalName));
                        //create a new copy
                        remoteRepo.Copy (copys [0], file);
                        
                        //delete copys that is not in local repo and not is a trash file
                        foreach (StorageQloudObject remote in copys) {
                            if (!System.IO.File.Exists (remote.FullLocalName) && !remote.InTrash){
                                remoteRepo.MoveFileToTrash (remote);
                            }
                        }
                        //if there was a rename or move, the hash remains the same 
                        BacklogSynchronizer.GetInstance ().EditFileByHash (file);
                        ChangesInLastSync.Add (new Change (file, WatcherChangeTypes.Created));
                    } else {
                        //Update

                        if (DownloadSynchronizer.GetInstance ().ChangesInLastSync.Any (c => c.File.AbsolutePath == file.AbsolutePath && c.Event == WatcherChangeTypes.Created))
                            return true;
                    
                        if (remoteRepo.Files.Any (rf => rf.AbsolutePath == file.AbsolutePath && rf.RemoteMD5Hash != file.LocalMD5Hash)) {                            
                            Logger.LogInfo ("Synchronizing Remote",string.Format("Update: {0}",file.FullLocalName));
                            remoteRepo.MoveFileToTrash (file);
                            remoteRepo.Upload (file);
                            //hash changes then must be by name
                            BacklogSynchronizer.GetInstance ().EditFileByName (file);
                            ChangesInLastSync.Add (new Change (file, WatcherChangeTypes.Changed));
                        }
                        //Create
                        else if (!remoteRepo.Files.Any (rf => rf.AbsolutePath == file.AbsolutePath)) {
                            if (DownloadSynchronizer.GetInstance ().ChangesInLastSync.Any (c => c.File.AbsolutePath == file.AbsolutePath && c.Event == WatcherChangeTypes.Created))
                                return true;
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
                    if (DownloadSynchronizer.GetInstance().ChangesInLastSync.Any(c=> c.File.AbsolutePath==file.AbsolutePath && c.Event == WatcherChangeTypes.Deleted))
                        return true;
                    
                    Logger.LogInfo ("Synchronizing Remote",string.Format("Delete: {0}",file.FullLocalName));
                    if (remoteRepo.FolderExistsInBucket (file)){

                        DeleteFolder(file);
                    }
                    else{

                        DeleteFile (file);
                    }
                }
               
            } catch (Exception e) {
                Logger.LogInfo ("UploadSynchronizer", e);
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
    }
}

