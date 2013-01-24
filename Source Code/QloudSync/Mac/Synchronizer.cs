

using Amazon.S3.Model;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

using QloudSync.Repository;
using QloudSync.Util;



namespace  QloudSync.Synchrony
{
    public abstract class Synchronizer
    {
        protected List <QloudSync.Repository.File> localFiles;
        protected List <RemoteFile> remoteFiles;
        protected List <Folder> localEmptyFolders;
        
        protected int countOperation = 0;
 
        
        protected Synchronizer ()
        {
        }
        
        public bool Synchronized {
             set; get;
        }
        
        public static double SyncSize{
            protected set; get;
        }
        
        public abstract bool Synchronize();
        
        public List<RemoteFile> RemoteChanges {
            get {
                TimeSpan diffClocks = RemoteRepo.DiffClocks;
				DateTime referencialClock = Repo.LastSyncTime.Subtract (diffClocks);
                return RemoteRepo.Files.Where (rf => Convert.ToDateTime (rf.AsS3Object.LastModified).Subtract (referencialClock).TotalSeconds > 0).ToList<RemoteFile>();
            }
        }
        
        public bool HasRemoteChanges {
            get {
                return RemoteChanges.Count != 0;
            }
        }
        
        protected bool Initialize ()
        {

            //ServicePointManager.ServerCertificateValidationCallback = connection.GetValidationCallBack;

            if ( RemoteRepo.InitBucket () && RemoteRepo.InitTrashFolder ()) {

                //pegar os posteriores a ultima sincronizaçao
                /*DirectoryInfo dir = new DirectoryInfo (LocalRepo.LocalFolder);
                localFiles = LocalFile.Get ( dir.GetFiles ("*", SearchOption.AllDirectories).ToList ());
                localFiles.AddRange ( Folder.Get (dir.GetDirectories ("*", SearchOption.AllDirectories).ToList ()));
				//LocalRepo.Files = OSXFileWatcher.*/
				localFiles = LocalRepo.Files;
                remoteFiles = RemoteRepo.Files;
                
                localEmptyFolders = LocalRepo.EmptyFolders;
                countOperation = 0;

                return true;
            }
            else
            {
                Logger.LogInfo("Synchronizer", "Could not create the bucket correctly");
                return false;
            }
        }   
        
        protected void ShowDoneMessage (string action)
        {
            if (countOperation == 0)
                Logger.LogInfo ("Synchronizer", "Files up to date.\n");
            else
                Logger.LogInfo("Synchronizer", action+" successful: "+countOperation+" files.\n");
        }   
        
        protected bool ExistsInBucket (QloudSync.Repository.File  file){
            /*foreach (RemoteFile remote in remoteFiles){
                Console.WriteLine (remote.AbsolutePath+" "+ file.AbsolutePath);
               // if (remote.AbsolutePath == file.AbsolutePath || remote.FullLocalName.Contains(file.FullLocalName)){
                    //return true;
                //}
                    
            }//return false;*/
             return remoteFiles.Where (rf => rf.AbsolutePath ==  file.AbsolutePath
                                      || rf.FullLocalName.Contains ( file.FullLocalName)).Any ();
        }

		protected bool ExistsVersion (QloudSync.Repository.File file)
		{
			return RemoteRepo.TrashFiles.Where (tf => tf.AbsolutePath == file.AbsolutePath+"(1)").Any ();
		}


        

        public bool FilesIsSync (LocalFile localFile, RemoteFile remoteFile)
        {
            return localFile.MD5Hash == remoteFile.MD5Hash;
        }
    }
}