using System;
using System.Collections.Generic;
using GreenQloud.Repository;
using System.Threading;
using System.IO;
using GreenQloud.Synchrony;
using System.Linq;

namespace GreenQloud
{
    public class UploadController : SynchronizerController
    {
        public ChangesCapturedByWatcher<GreenQloud.Repository.File> PendingChanges;
        OSXFileSystemWatcher watcher = new OSXFileSystemWatcher ();
        int controller = 0;

        protected UploadController ()
        {
            try {            
                watcher.Changed += delegate(string path) {
                    LocalFile changedFile = new LocalFile (path);
                    PendingChanges.Add (changedFile);
                    if (PendingChanges.Contains(changedFile))
                        new Thread (Synchronize).Start ();
                };
                PendingChanges = new ChangesCapturedByWatcher<GreenQloud.Repository.File> ();

            } catch (Exception e) {
                Logger.LogInfo("UploadController", e);
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
            ++controller;
            if (Status == SyncStatus.Idle && controller == 1) {
                Status = SyncStatus.Sync;
                int index = 0;
                while (PendingChanges.Count != 0) {
                    
                    if (index >= PendingChanges.Count)
                        break;
                    GreenQloud.Repository.File pendingFile = PendingChanges[index];

                    if (UploadSynchronizer.GetInstance().Synchronize(pendingFile))
                    {   
                        PendingChanges.Remove (pendingFile); 
                        index = 0;
                    }
                    else 
                        index++;
                }
                controller = 0;
                Status = SyncStatus.Idle;
            }            
        }

        public class ChangesCapturedByWatcher <File> : List<GreenQloud.Repository.File>
        {
            public event EventHandler OnAdd;
            
            public new void Add (GreenQloud.Repository.File item)
            {
                if (null != OnAdd) {
                    OnAdd (this, null);                
                }
                if (item.FullLocalName == RuntimeSettings.HomePath)
                    return;
                if (Directory.Exists (item.FullLocalName))
                    base.Add (item.ToFolder());
                else if (!this.Any (f => f.FullLocalName == item.FullLocalName || item.FullLocalName.Contains (f.FullLocalName + "."))) {
                    base.Add (item);
                }
            }
        }       
    }
}