using System;
using System.Collections.Generic;
using GreenQloud.Repository;
using System.Threading;
using System.IO;
using GreenQloud.Synchrony;
using System.Linq;

namespace GreenQloud
{
    public class RemoteController : SynchronizerController
    {
        public ChangesCapturedByWatcher<StorageQloudObject> PendingChanges;
        OSXFileSystemWatcher watcher = new OSXFileSystemWatcher ();
        int controller = 0;

        protected RemoteController ()
        {
           try {            
                watcher.Changed += delegate(string path) {
                    if(BacklogSynchronizer.GetInstance().Done){
                        StorageQloudObject changedFile = new StorageQloudObject (path);
                        PendingChanges.Add (changedFile);
                        if (PendingChanges.Contains(changedFile)){
                            RemoteSynchronizer.GetInstance().PendingFiles.Add(changedFile);
                            if(Directory.Exists(path)){
                                foreach (string pathFile in Directory.GetFiles(path)){
                                    RemoteSynchronizer.GetInstance().PendingFiles.Add(new StorageQloudObject(pathFile));                            
                                }
                            }
                            new Thread (Synchronize).Start ();
                        }
                    }
                };
                PendingChanges = new ChangesCapturedByWatcher<StorageQloudObject> ();

            } catch (Exception e) {
                Logger.LogInfo("UploadController", e);
            }
        }

        private static RemoteController instance;
        public static RemoteController GetInstance(){
            if(instance == null)
                instance = new RemoteController();
            return instance;
        }

        public override void Synchronize ()
        {
            ++controller;
            if (Status == SyncStatus.Idle && controller == 1) {

                int index = 0;
                while (PendingChanges.Count != 0) {
                    Status = SyncStatus.Sync;    
                    if (index >= PendingChanges.Count)
                        break;
                    StorageQloudObject pendingFile = PendingChanges[index];

                    if (RemoteSynchronizer.GetInstance().Synchronize(pendingFile))
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

        public void Stop ()
        {
            watcher.Stop();
        }

        public class ChangesCapturedByWatcher <File> : List<StorageQloudObject>
        {
            public event EventHandler OnAdd;
            
            public new void Add (StorageQloudObject item)
            {
                if (null != OnAdd) {
                    OnAdd (this, null);                
                }
                if (item.FullLocalName == RuntimeSettings.HomePath)
                    return;
                //if (Directory.Exists (item.FullLocalName))
                  //  base.Add (item.ToFolder());
                //else
                if (!this.Any (f => f.FullLocalName == item.FullLocalName || item.FullLocalName.Contains (f.FullLocalName + "."))) {
                    base.Add (item);
                }
            }
        }       
    }
}