

using Amazon.S3.Model;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

using GreenQloud.Repository;
using GreenQloud.Util;



namespace GreenQloud.Synchrony
{
    public abstract class Synchronizer
    {

        private List<GreenQloud.Repository.Change> changesInLastSync = new List<GreenQloud.Repository.Change>();

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
        

        public long BytesTransferred {
            get{
                return remoteRepo.Connection.TransferSize;
            }
        }
        
        public List<GreenQloud.Repository.Change> ChangesInLastSync {
            set {
                this.changesInLastSync = value;
            }get {
                return changesInLastSync;
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
        
        protected bool ExistsInBucket (GreenQloud.Repository.File  file){
             return remoteFiles.Where (rf => rf.AbsolutePath ==  file.AbsolutePath
                                      || rf.FullLocalName.Contains ( file.FullLocalName)).Any ();
        }

        protected bool ExistsVersion (GreenQloud.Repository.File file)
		{
			return remoteRepo.TrashFiles.Any (tf => tf.AbsolutePath == file.AbsolutePath+"(1)");
		}


        

        public static bool FilesIsSync (LocalFile localFile, RemoteFile remoteFile)
        {
            return localFile.MD5Hash == remoteFile.MD5Hash;
        }
    }
}