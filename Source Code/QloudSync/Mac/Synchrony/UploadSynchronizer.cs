using System;

using System.IO;
using System.Linq;
using System.Collections.Generic;
using GreenQloud.Synchrony;
using GreenQloud.Repository;
using GreenQloud.Util;

namespace GreenQloud.Synchrony
{
    public class UploadSynchronizer : Synchronizer
    {
        private static UploadSynchronizer instance;

        protected UploadSynchronizer ()
        {

        }

        public static UploadSynchronizer GetInstance ()
        {
            if (instance == null) {
                instance = new UploadSynchronizer ();
            }
            return instance;
        }

        public bool Synchronize (GreenQloud.Repository.File file)
        {
            try{        
                if(file.IsIgnoreFile)
                    return true;
                if(file.IsAFolder)
                    SynchronizeFolder(file);
                else{
                    SynchronizeFile(file);
                }
            } catch (Exception e) {
                Logger.LogInfo ("UploadSynchronizer", e);
                return false;
            }
            return true;
        }

        void SynchronizeFile (GreenQloud.Repository.File file)
        {
            if (System.IO.File.Exists(file.FullLocalName))
            {
                //Rename and moves
                //file already exists in remote?
                if (remoteRepo.AllFiles.Any (rf => rf.MD5Hash == file.MD5Hash)) {
                    //get all remote copys of file
                    List<RemoteFile> copys = remoteRepo.AllFiles.Where(rf => rf.MD5Hash == file.MD5Hash).ToList<RemoteFile>();
                    //create a new copy
                    remoteRepo.Copy(copys[0], file);
                    //delete copys that is not in local repo and not is a trash file
                    foreach (RemoteFile remote in copys){
                        if (!System.IO.File.Exists(remote.FullLocalName) && !remote.InTrash)
                            remoteRepo.MoveToTrash (remote);
                    }
                    //if there was a rename or move, the hash remains the same 
                    BacklogSynchronizer.GetInstance().EditFileByHash (file);
                    ChangesInLastSync.Add (new Change(file, WatcherChangeTypes.Renamed));
                }

                else{
                    //Update
                    if (remoteRepo.Files.Where (rf=> rf.AbsolutePath == file.AbsolutePath).Any()){
                        RemoteFile remoteFile = new RemoteFile (file.AbsolutePath);
                        remoteRepo.MoveToTrash (remoteFile);
                        remoteRepo.Upload (file);
                        //hash changes then must be by name
                        BacklogSynchronizer.GetInstance().EditFileByName(file);
                        ChangesInLastSync.Add (new Change(file, WatcherChangeTypes.Changed));
                    }
                    //Create
                    else{
                        if (DownloadSynchronizer.GetInstance().ChangesInLastSync.Any(c=> c.File.AbsolutePath==file.AbsolutePath && c.Event == WatcherChangeTypes.Created))
                            return;
                        remoteRepo.Upload (file) ; 
                        ChangesInLastSync.Add (new Change(file, WatcherChangeTypes.Created));
                        BacklogSynchronizer.GetInstance().AddFile(file);
                    }
                }
            //Delete
            }else{
                if (DownloadSynchronizer.GetInstance().ChangesInLastSync.Any(c=> c.File.AbsolutePath==file.AbsolutePath && c.Event == WatcherChangeTypes.Deleted))
                    return;
                DeleteFile (file);
            }
        }

        void SynchronizeFolder (GreenQloud.Repository.File file)
        {
            if (Directory.Exists(file.FullLocalName)){
                if (!remoteRepo.Files.Where (remo => remo.AbsolutePath.Contains (file.AbsolutePath)).Any())
                    remoteRepo.CreateFolder (file.ToFolder());
            }
            else{
                DeleteFolder (file.ToFolder());
            }
        }

        public bool DeleteFolder (Folder folder)
        {

            try {
                List<RemoteFile> filesInFolder = remoteRepo.Files.Where (remo => remo.AbsolutePath.Contains (folder.AbsolutePath)).ToList<RemoteFile> ();

                if (filesInFolder.Count >= 1) {
                    foreach (RemoteFile r in filesInFolder) {
                        if (folder.IsIgnoreFile)
                            continue;
                        if (r.AbsolutePath != folder.AbsolutePath)
                            DeleteFile(r);

                    }
                }
                remoteRepo.MoveToTrash (folder);
                BacklogSynchronizer.GetInstance().RemoveFileByAbsolutePath(folder);
            } catch {
                return false;
            }
            return true;
        }

        void DeleteFile(GreenQloud.Repository.File file){
            remoteRepo.MoveToTrash (file);
            ChangesInLastSync.Add (new Change(file, WatcherChangeTypes.Deleted));
            BacklogSynchronizer.GetInstance().RemoveFileByAbsolutePath(file);
        }

    }
}

