using System;
using GreenQloud.Repository.Local;
using GreenQloud.Persistence;
using GreenQloud.Model;
using System.Threading;
using GreenQloud.Persistence.SQLite;
using System.Collections.Generic;
using System.IO;
using GreenQloud.Repository;

namespace GreenQloud.Synchrony
{
    public class StorageQloudLocalEventsSynchronizer : AbstractLocalEventsSynchronizer
    {
        static StorageQloudLocalEventsSynchronizer instance;

        Dictionary<string, QloudSyncFileSystemWatcher> watchers;
        SQLiteRepositoryDAO repositoryDAO = new SQLiteRepositoryDAO();
        SQLiteEventDAO eventDAO = new SQLiteEventDAO();
        Thread watchersThread;

        
 
        public event Action Failed = delegate { };
        
        public event FinishedEventHandler Finished = delegate { };
        public delegate void FinishedEventHandler ();

        private Event LastLocalEvent = new Event();
        private DateTime LastTimeSync = new DateTime();
        private StorageQloudLocalEventsSynchronizer 
            (LogicalRepositoryController logicalLocalRepository, IPhysicalRepositoryController physicalLocalRepository, RemoteRepositoryController remoteRepository, EventDAO eventDAO, RepositoryItemDAO repositoryItemDAO) :
                base (logicalLocalRepository, physicalLocalRepository, remoteRepository, eventDAO, repositoryItemDAO)
        {
            watchersThread = new Thread(()=>{
                watchers = new Dictionary<string, QloudSyncFileSystemWatcher>();
                foreach (LocalRepository repo in repositoryDAO.All){ 
                    QloudSyncFileSystemWatcher watcher = new QloudSyncFileSystemWatcher(repo.Path);
                    watcher.Changed += delegate(Event e) {
                        Logger.LogEvent("EVENT FOUND", e);

                        if(Working) 
                        {
                            try{
                                CreateEvent (e);
                            }catch (DisconnectionException)
                            {
                                //SyncStatus = SyncStatus.IDLE;
                                Program.Controller.HandleDisconnection();
                            }
                        }
                    };
                    watchers.Add (repo.Path, watcher);
                }
            
            });
        }

        public static StorageQloudLocalEventsSynchronizer GetInstance(){
            if (instance == null)
                instance = new StorageQloudLocalEventsSynchronizer (new StorageQloudLogicalRepositoryController(), 
                                                                    new StorageQloudPhysicalRepositoryController(),
                                                                    new RemoteRepositoryController(),
                                                                    new SQLiteEventDAO (),
                                                                    new SQLiteRepositoryItemDAO());
            return instance;
        }

        public QloudSyncFileSystemWatcher GetWatcher(string path){
            QloudSyncFileSystemWatcher watcher = null;
            foreach(string watcherKey in watchers.Keys){
                if (path.StartsWith (watcherKey)) {
                    watchers.TryGetValue (watcherKey, out watcher);
                    return watcher;
                }
            }
            return watcher;
        }

        void CreateEvent (Event e)
        {
            Create(e);

            LastLocalEvent = e;
            LastTimeSync = GlobalDateTime.Now;
        }

        public new void Start ()
        {
            try{
                watchersThread.Start();
                base.Start();
            }catch{
                // do nothing
            }
            Working = true;
        }

        public override void Pause ()
        {
            Working = false;
        }

        public new void Stop ()
        {
            Working = false;
            if(watchersThread.IsAlive)
                watchersThread.Join();
            foreach (QloudSyncFileSystemWatcher watcher in watchers.Values)
                watcher.Stop();
            base.Stop();
        }

        public ThreadState ControllerStatus{
            get{
                return watchersThread.ThreadState;
            }
        }

        protected List<string> warnings = new List<string> ();
        protected List<string> errors   = new List<string> ();

        public string [] Warnings {
            get {
                return this.warnings.ToArray ();
            }
        }
        
        public string [] Errors {
            get {
                return this.errors.ToArray ();
            }
        }
    }
}

