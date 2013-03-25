using System;
using GreenQloud.Repository.Local;
using GreenQloud.Repository.Remote;
using GreenQloud.Persistence;
using GreenQloud.Model;
using System.Threading;
using GreenQloud.Persistence.SQLite;
using System.Collections.Generic;

namespace GreenQloud.Synchrony
{
    public class StorageQloudLocalEventsSynchronizer : LocalEventsSynchronizer
    {
        static StorageQloudLocalEventsSynchronizer instance;

        List<OSXFileSystemWatcher> watchers;
        SQLiteRepositoryDAO repositoryDAO = new SQLiteRepositoryDAO();
        Thread watchersThread;

        private StorageQloudLocalEventsSynchronizer 
            (LogicalRepositoryController logicalLocalRepository, PhysicalRepositoryController physicalLocalRepository, RemoteRepositoryController remoteRepository, TransferDAO transferDAO, EventDAO eventDAO) :
                base (logicalLocalRepository, physicalLocalRepository, remoteRepository, transferDAO, eventDAO)
        {
            watchersThread = new Thread(()=>{
                watchers = new List<OSXFileSystemWatcher>();
                foreach (LocalRepository repo in repositoryDAO.All){ 
                    OSXFileSystemWatcher watcher = new OSXFileSystemWatcher(repo.Path);
                    watcher.Changed += delegate(string path) {
                        if(Working) 
                            Synchronize( physicalLocalRepository.CreateObjectInstance (path));                
                    };
                    watchers.Add (watcher);
                }
            });
        }

        public static StorageQloudLocalEventsSynchronizer GetInstance(){
            if (instance == null)
                instance = new StorageQloudLocalEventsSynchronizer (new StorageQloudLogicalRepositoryController(), 
                                                                    new StorageQloudPhysicalRepositoryController(),
                                                                    new StorageQloudRemoteRepositoryController(),
                                                                    new SQLiteTransferDAO (),
                                                                    new SQLiteEventDAO ());
            return instance;
        }

        bool working;

        public new void Start ()
        {
            try{
                watchersThread.Start();
                base.Start();
            }catch{
                // do nothing
            }
            working = true;
        }

        public override void Pause ()
        {
            working = false;
        }

        public new void Stop ()
        {
            working = false;
            watchersThread.Join();
            foreach (OSXFileSystemWatcher watcher in watchers)
                watcher.Stop();
            base.Stop();
        }

        public ThreadState ControllerStatus{
            get{
                return watchersThread.ThreadState;
            }
        }

        public bool Working{
            get{
                return working;
            }
        }
    }
}

