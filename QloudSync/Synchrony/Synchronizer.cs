using Amazon.S3.Model;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Threading;

using GreenQloud.Model;
using GreenQloud.Repository;
using GreenQloud.Util;

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
        public delegate void ProgressChangedEventHandler (double percentage, double time);

        protected Synchronizer ()
        {
        }

        #region Abstract Methods
        
        public abstract void Synchronize();
        public abstract void Synchronize(Event e);
        public abstract Event GetEvent (RepositoryItem item, RepositoryType type);

        public abstract void Start ();
        public abstract void Pause ();
        public abstract void Stop ();

        #endregion

        #region Implemented Methods

        private SyncStatus status;
        public delegate void SyncStatusChangedHandler (SyncStatus status);
        public event SyncStatusChangedHandler SyncStatusChanged = delegate {};

        public SyncStatus Status {
            get {
                return status;
            }
            set {
                status = value;
                SyncStatusChanged(status);
            }
        }

        public bool Done {
            set; get;
        }

        int countOperation = 0;

        protected void ShowDoneMessage (string action)
        {
            if (countOperation == 0)
                Logger.LogInfo (action, "Files up to date.\n");
            else
                Logger.LogInfo(action, string.Format("Successful: {0} files.\n",countOperation));
        }   
        #endregion
    }
}