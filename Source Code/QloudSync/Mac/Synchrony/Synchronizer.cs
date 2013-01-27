

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

        private List<QloudSync.Repository.File> filesinlastsync = new List<QloudSync.Repository.File>();

        protected List <QloudSync.Repository.File> localFiles;
        protected List <RemoteFile> remoteFiles;
        protected List <Folder> localEmptyFolders;
        protected RemoteRepo remoteRepo;
        
        protected int countOperation = 0;
 
        
        public DateTime LastSyncTime {
            get;
            set;
        }
        
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

        
        #endregion

        
        public bool Synchronized {
             set; get;
        }
        
        public static double SyncSize{
            protected set; get;
        }
                  
        protected void Initialize ()
        {
       
        	localFiles = LocalRepo.Files;
            remoteFiles = remoteRepo.Files;
            
            localEmptyFolders = LocalRepo.EmptyFolders;
            countOperation = 0;


        }   
        
        protected void ShowDoneMessage (string action)
        {
            if (countOperation == 0)
                Logger.LogInfo (action, "Files up to date.\n");
            else
                Logger.LogInfo(action, string.Format("Successful: {0} files.\n",countOperation));
        }   
        
        protected bool ExistsInBucket (QloudSync.Repository.File  file){
             return remoteFiles.Where (rf => rf.AbsolutePath ==  file.AbsolutePath
                                      || rf.FullLocalName.Contains ( file.FullLocalName)).Any ();
        }

		protected bool ExistsVersion (QloudSync.Repository.File file)
		{
			return remoteRepo.TrashFiles.Where (tf => tf.AbsolutePath == file.AbsolutePath+"(1)").Any ();
		}


        

        public static bool FilesIsSync (LocalFile localFile, RemoteFile remoteFile)
        {
            return localFile.MD5Hash == remoteFile.MD5Hash;
        }
    }
}