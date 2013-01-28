using System;
using QloudSync.Synchrony;
using System.IO;
using QloudSync.Repository;

namespace QloudSync
{
    public class Test
    {
        public Test ()
        {
        }

        public void ClearRepositories (RemoteRepo remoteRepo)
        {
            
            remoteRepo.DeleteAllFilesInBucket();
            ClearFolder (RuntimeSettings.HomePath);
            BacklogSynchronizer.GetInstance().Create();
            BacklogSynchronizer.GetInstance().RemoveAllFiles();
        }
        
        void ClearFolder(string path){
            if (Directory.Exists (path)) {
                foreach (string file in Directory.GetFiles(path))
                    System.IO.File.Delete(file);
                foreach (string folder in Directory.GetDirectories(path))
                {
                    ClearFolder(folder);
                    Directory.Delete (folder);
                }
            }
        }

    }
}

