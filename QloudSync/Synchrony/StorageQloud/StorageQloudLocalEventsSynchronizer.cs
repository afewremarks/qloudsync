using System;
using GreenQloud.Repository.Local;
using GreenQloud.Repository.Remote;
using GreenQloud.Persistence;
using GreenQloud.Model;
using System.Threading;
using GreenQloud.Persistence.SQLite;
using System.Collections.Generic;
using System.IO;

namespace GreenQloud.Synchrony
{
    public class StorageQloudLocalEventsSynchronizer : AbstractLocalEventsSynchronizer
    {
        static StorageQloudLocalEventsSynchronizer instance;

        Dictionary<string, OSXFileSystemWatcher> watchers;
        SQLiteRepositoryDAO repositoryDAO = new SQLiteRepositoryDAO();
        SQLiteEventDAO eventDAO = new SQLiteEventDAO();
        Thread watchersThread;

        
 
        public event Action Failed = delegate { };
        
        public event FinishedEventHandler Finished = delegate { };
        public delegate void FinishedEventHandler ();

        private Event LastLocalEvent = new Event();
        private DateTime LastTimeSync = new DateTime();
        private StorageQloudLocalEventsSynchronizer 
            (LogicalRepositoryController logicalLocalRepository, PhysicalRepositoryController physicalLocalRepository, RemoteRepositoryController remoteRepository, TransferDAO transferDAO, EventDAO eventDAO, RepositoryItemDAO repositoryItemDAO) :
                base (logicalLocalRepository, physicalLocalRepository, remoteRepository, transferDAO, eventDAO, repositoryItemDAO)
        {

            RepositoryItem r = new RepositoryItem();
            r.Name = string.Empty;
            r.RelativePath = string.Empty;
            r.Repository = new LocalRepository (string.Empty);
            LastLocalEvent.Item = r;
            watchersThread = new Thread(()=>{
                watchers = new Dictionary<string, OSXFileSystemWatcher>();
                foreach (LocalRepository repo in repositoryDAO.All){ 
                    OSXFileSystemWatcher watcher = new OSXFileSystemWatcher(repo.Path);
                    watcher.Changed += delegate(string path) {
                        Logger.LogInfo("Event found",path);
                        if(Working) 
                        {
                            try{
                                CreateEvent (path);
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
                                                                    new StorageQloudRemoteRepositoryController(),
                                                                    new SQLiteTransferDAO (),
                                                                    new SQLiteEventDAO (),
                                                                    new SQLiteRepositoryItemDAO());
            return instance;
        }

        public OSXFileSystemWatcher GetWatcher(string path){
            OSXFileSystemWatcher watcher;
            watchers.TryGetValue (path, out watcher);
            return watcher;
        }

        void CreateEvent (string path)
        {
            RepositoryItem item;
            LocalRepository repo = repositoryDAO.GetRepositoryByItemFullName (path);

            if (Directory.Exists(path)){
                item = RepositoryItem.CreateInstance (repo, path, true, 0, GlobalDateTime.NowUniversalString);
            }
            else if (File.Exists (path)){
                item = RepositoryItem.CreateInstance (repo, path, false, 0, GlobalDateTime.NowUniversalString);
            }else{
                item = RepositoryItem.CreateInstance (repo, path, false, 0, GlobalDateTime.NowUniversalString);
                item.IsAFolder = new SQLiteRepositoryItemDAO().IsFolder(item);
            }

            if(!item.IsIgnoreFile){
                Event e = new Event();
                e.Item = item;
                e.RepositoryType = RepositoryType.LOCAL;
                Synchronize(e);

                LastLocalEvent = e;
                LastTimeSync = GlobalDateTime.Now;
            }
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
            watchersThread.Join();
            foreach (OSXFileSystemWatcher watcher in watchers.Values)
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

