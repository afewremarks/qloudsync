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

        public bool CreateFile (QloudSync.Repository.File file)
        {
            try{
                //Rename and moves
                //file already exists in remote?
                if (remoteRepo.AllFiles.Where (rf => rf.MD5Hash == file.MD5Hash).Any()) {
                    //get all remote copys of file
                    List<RemoteFile> copys = remoteRepo.AllFiles.Where(rf => rf.MD5Hash == file.MD5Hash).ToList<RemoteFile>();
                    //create a new copy
                    remoteRepo.Copy(copys[0], file);
                    //delete copys that is not in local repo and not is a trash file
                    foreach (RemoteFile remote in copys){
                        if (!System.IO.File.Exists(remote.FullLocalName) && !remote.InTrash)
                            remoteRepo.Delete (remote);
                    }
                    //if there was a rename or move, the hash remains the same 
                    BacklogSynchronizer.GetInstance().EditFileByHash (file);
                }
                //Create
                else{
                    if (file.IsAFolder)
                        remoteRepo.CreateFolder (new Folder (file.FullLocalName));
                    else
                        remoteRepo.Upload (file) ;

                    BacklogSynchronizer.GetInstance().AddFile(file);
                }
            } catch (Exception e) {
                Logger.LogInfo ("UploadSynchronizer", e);
                return false;
            }
            return true;
        }
        
        public bool UpdateFile (QloudSync.Repository.File file)
        {
            try {
                RemoteFile remoteFile = new RemoteFile (file.AbsolutePath);
                remoteRepo.MoveToTrash (remoteFile);
                remoteRepo.Upload (file);
                //hash changes then must be by name
                BacklogSynchronizer.GetInstance().EditFileByName(file);
                return true;
            } catch {
                return false;
            }       
        }

        public bool DeleteFile (QloudSync.Repository.File file)
        {

            try {
                string absolutePath = string.Format("{0}{1}", file.AbsolutePath, Path.PathSeparator);
                List<RemoteFile> filesInFolder = remoteRepo.Files.Where (remo => remo.AbsolutePath.Contains (absolutePath)).ToList<RemoteFile> ();

                if (filesInFolder.Count >= 1) {
                    file = new Folder (absolutePath);
                    foreach (RemoteFile r in filesInFolder) {
                        if (file.IsIgnoreFile)
                            continue;
                        if (r.AbsolutePath != file.AbsolutePath)
                            remoteRepo.MoveToTrash (r);

                    }
                }
                remoteRepo.MoveToTrash (file);
                BacklogSynchronizer.GetInstance().RemoveFileByAbsolutePath(file);
            } catch {
                return false;
            }
            return true;
        }

    }
}

