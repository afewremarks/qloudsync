using System;
using QloudSync.Repository;
using System.Collections.Generic;

namespace QloudSync
{

    public enum SyncStatus {
        Idle,
        Sync,
        Error
    }

    public abstract class SynchronizerController
    {


        public delegate void SyncStatusChangedHandler (SyncStatus status);
        public event SyncStatusChangedHandler SyncStatusChanged = delegate {};
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

        protected SynchronizerController ()
        {
            ClearDownloadIndexes ();
        }


        protected double Percent {
            set; get;
        }
        
        protected double Speed {
            set; get;
        }
        
        protected int TimeRemaining {
            set; get;
        }

        protected void ClearDownloadIndexes()
        {
            Percent = 0;
            Speed = 0;
            TimeRemaining = 0;
        }

        public abstract void Synchronize ();

    }
}

