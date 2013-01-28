using System;
using System.Collections.Generic;
using QloudSync.Repository;
using System.Threading;
using System.IO;
using QloudSync.Synchrony;
using System.Linq;

namespace QloudSync
{
    public class UploadController : SynchronizerController
    {
        public ChangesCapturedByWatcher<Change> PendingChanges;



        protected UploadController(){
            new IO.OSXFileWatcher(RuntimeSettings.HomePath);
            PendingChanges = new ChangesCapturedByWatcher<Change>();
            PendingChanges.OnAdd += HandleOnAdd;
        }

        void HandleOnAdd (object sender, EventArgs e)
        {
            if (Status == SyncStatus.Idle) {
                Status = SyncStatus.Sync;
                new Thread (Synchronize).Start ();
                Status = SyncStatus.Idle;
            }

        }

        private static UploadController instance;
        
        public static UploadController GetInstance(){
            if(instance == null)
                instance = new UploadController();
            return instance;
        }

        public override void Synchronize ()
        {
            
            int attempt = 0;
            int index = 0;
            Console.WriteLine (DateTime.Now.ToUniversalTime () + " - Pending Changes (" + PendingChanges.Count + ") ");
            
            while (PendingChanges.Count != 0) {
                
                if (index >= PendingChanges.Count)
                    break;
                
                
                Change change = PendingChanges [index];
                if(change != null)
                {
                    bool operationSuccessfully = false;
                    
                    switch (change.Event) {
                    case WatcherChangeTypes.Deleted:
                        if (PendingChanges.Count (c=> c.File.MD5Hash == change.File.MD5Hash)>1)
                            operationSuccessfully = true;
                        else
                            operationSuccessfully = UploadSynchronizer.GetInstance().DeleteFile (change.File);
                        break;
                    case WatcherChangeTypes.Created:
                        operationSuccessfully = UploadSynchronizer.GetInstance().CreateFile (change.File);
                        break;
                    case WatcherChangeTypes.Changed:
                        if (PendingChanges.Count(c=> c.File.MD5Hash == change.File.MD5Hash && c.Event == WatcherChangeTypes.Changed)>1)
                            operationSuccessfully = true;
                        else
                            operationSuccessfully = UploadSynchronizer.GetInstance().UpdateFile (change.File);
                        break;
                    }
                    
                    if (operationSuccessfully) {
                        PendingChanges.Remove (change);
                        attempt = 0;
                        index = 0;
                    } else {
                        Console.WriteLine ("Fail "+change.Event+" to "+change.File.FullLocalName);
                        if (attempt >= 3) {
                            index++;
                            attempt = 0;
                        } else
                            attempt ++;
                    }
                }
            }
            
        }


        public class ChangesCapturedByWatcher<Change> : List<Change>
        {
            public event EventHandler OnAdd;
            
            public new void Add(Change item) {
                if (null != OnAdd) {
                    OnAdd(this, null);
                }
                base.Add(item);
            }
        }
    }
}

