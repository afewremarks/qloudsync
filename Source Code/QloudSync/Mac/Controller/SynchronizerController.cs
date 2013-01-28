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

        public SyncStatus Status
        {
            get; set;
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

