

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

        protected List <StorageQloudObject> remoteFiles;
        protected List <StorageQloudObject> remoteFolders;
        protected List <StorageQloudObject> localEmptyFolders;
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
            remoteFolders = remoteRepo.Folders;

            countOperation = 0;
        }   
        
        protected void ShowDoneMessage (string action)
        {
            if (countOperation == 0)
                Logger.LogInfo (action, "Files up to date.\n");
            else
                Logger.LogInfo(action, string.Format("Successful: {0} files.\n",countOperation));
        }   

        protected bool ExistsInBucket (StorageQloudObject folder)
        {
            return remoteFolders.Any (rf=> rf.AbsolutePath==folder.AbsolutePath || rf.AbsolutePath==folder.AbsolutePath+"/");
        }

        protected bool ExistsVersion (StorageQloudObject file)
		{
			return remoteRepo.TrashFiles.Any (tf => tf.AbsolutePath == file.AbsolutePath+"(1)");
		}        

 
    }
}