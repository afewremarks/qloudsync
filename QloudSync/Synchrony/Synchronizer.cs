

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
using System.Threading;



namespace GreenQloud.Synchrony
{
    
    public enum SyncStatus{
        IDLE,
        UPLOADING,
        DOWNLOADING,
        VERIFING
    }

    public abstract class Synchronizer
    {

        private List<GreenQloud.Repository.Change> changesInLastSync = new List<GreenQloud.Repository.Change>();

        protected List <StorageQloudObject> remoteFiles;
        protected List <StorageQloudObject> remoteFolders;
        protected List <StorageQloudObject> localEmptyFolders;
        protected RemoteRepo remoteRepo;
        
        public delegate void SyncStatusChangedHandler (SyncStatus status);
        public event SyncStatusChangedHandler SyncStatusChanged = delegate {};

        public event ProgressChangedEventHandler ProgressChanged = delegate { };
        public delegate void ProgressChangedEventHandler (double percentage, double time);


        protected Synchronizer ()
        {
            remoteRepo = new StorageQloudRepo(); 
        }

        public abstract void Start ();
        public abstract void Pause ();
        public abstract void Stop ();

        public abstract void Synchronize();

        private SyncStatus status;
        public SyncStatus Status {
            get {
                return status;
            }
            set {
                status = value;
                SyncStatusChanged(status);
            }
        }

    
        public List<GreenQloud.Repository.Change> ChangesInLastSync {
            set {
                this.changesInLastSync = value;
            }get {
                return changesInLastSync;
            }
        }

        public DateTime LastSyncTime {
            get;
            set;
        }

        public bool Done {
            set; get;
        }


        int countOperation = 0;
        protected void Initialize ()
        {       
            countOperation = 0;
        	remoteFiles = remoteRepo.Files;
            remoteFolders = remoteRepo.Folders;
        }   
        
        protected void ShowDoneMessage (string action)
        {
            if (countOperation == 0)
                Logger.LogInfo (action, "Files up to date.\n");
            else
                Logger.LogInfo(action, string.Format("Successful: {0} files.\n",countOperation));
        }   
 
    }
}