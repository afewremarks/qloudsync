

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

        private List<QloudSync.Repository.File> filesinlastsync;

        protected List <QloudSync.Repository.File> localFiles;
        protected List <RemoteFile> remoteFiles;
        protected List <Folder> localEmptyFolders;
        protected RemoteRepo remoteRepo;
        
        protected int countOperation = 0;
 
        
        protected Synchronizer ()
        {
            remoteRepo = new RemoteRepo();
        }


        #region Properties
        public bool Done {
            set; get;
        }
        
        public double Size {
            set; get;
        }
        

        public int BytesTransferred {

            get{
                return remoteRepo.Connection.TransferSize;
            }
        }
        
        public List<QloudSync.Repository.File> FilesInLastSync {
            set {
                this.filesinlastsync = value;
            }get {
                return filesinlastsync;
            }
        }

        
        public SyncStatus Status
        {
            get; set;
        }
        
        #endregion

        
        public bool Synchronized {
             set; get;
        }
        
        public static double SyncSize{
            protected set; get;
        }
        
        public abstract void Synchronize();
                  
        protected bool Initialize ()
        {

            //ServicePointManager.ServerCertificateValidationCallback = connection.GetValidationCallBack;

            if ( remoteRepo.InitBucket () && remoteRepo.InitTrashFolder ()) {

                //pegar os posteriores a ultima sincronizaçao
                /*DirectoryInfo dir = new DirectoryInfo (LocalRepo.LocalFolder);
                localFiles = LocalFile.Get ( dir.GetFiles ("*", SearchOption.AllDirectories).ToList ());
                localFiles.AddRange ( Folder.Get (dir.GetDirectories ("*", SearchOption.AllDirectories).ToList ()));
				//LocalRepo.Files = OSXFileWatcher.*/
				localFiles = LocalRepo.Files;
                remoteFiles = remoteRepo.Files;
                
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
			return remoteRepo.TrashFiles.Where (tf => tf.AbsolutePath == file.AbsolutePath+"(1)").Any ();
		}


        

        public bool FilesIsSync (LocalFile localFile, RemoteFile remoteFile)
        {
            return localFile.MD5Hash == remoteFile.MD5Hash;
        }
    }
}