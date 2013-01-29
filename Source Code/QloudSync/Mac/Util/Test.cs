using System;
using QloudSync.Synchrony;
using System.IO;
using QloudSync.Repository;

namespace QloudSync
{
    public class Test
    {
        public QloudSync.IO.OSXFileWatcher w = new QloudSync.IO.OSXFileWatcher(RuntimeSettings.HomePath);
        protected RemoteRepo remoteRepo = new RemoteRepo();
        public Test ()
        {
        }

        public void ClearRepositories ()
        {
            w.Dispose();
            remoteRepo.DeleteAllFilesInBucket();
            ClearFolder (RuntimeSettings.HomePath);
            BacklogSynchronizer.GetInstance().Create();
            BacklogSynchronizer.GetInstance().RemoveAllFiles();

        }

        public void ClearLocalRepository ()
        {
            ClearFolder (RuntimeSettings.HomePath);
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

