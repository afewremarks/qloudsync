using System;
using GreenQloud.Synchrony;
using System.IO;
using GreenQloud.Repository;

namespace GreenQloud
{
    public class Test
    {
        protected RemoteRepo remoteRepo = new RemoteRepo();
        public Test ()
        {
        }

        public void ClearRepositories ()
        {
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

