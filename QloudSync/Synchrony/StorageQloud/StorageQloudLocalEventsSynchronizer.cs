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

        List<OSXFileSystemWatcher> watchers;
        SQLiteRepositoryDAO repositoryDAO = new SQLiteRepositoryDAO();
        SQLiteEventDAO eventDAO = new SQLiteEventDAO();
        Thread watchersThread;

        
 
        public event Action Failed = delegate { };
        
        public event FinishedEventHandler Finished = delegate { };
        public delegate void FinishedEventHandler ();

        private Event LastLocalEvent = new Event();
        private DateTime LastTimeSync = new DateTime();
        private StorageQloudLocalEventsSynchronizer 
            (LogicalRepositoryController logicalLocalRepository, PhysicalRepositoryController physicalLocalRepository, RemoteRepositoryController remoteRepository, TransferDAO transferDAO, EventDAO eventDAO) :
                base (logicalLocalRepository, physicalLocalRepository, remoteRepository, transferDAO, eventDAO)
        {

            RepositoryItem r = new RepositoryItem();
            Console.WriteLine("Creating LocalEvents");
            r.Name = string.Empty;
            r.RelativePath = string.Empty;
            r.Repository = new LocalRepository (string.Empty);
            LastLocalEvent.Item = r;
            watchersThread = new Thread(()=>{
                watchers = new List<OSXFileSystemWatcher>();
                foreach (LocalRepository repo in repositoryDAO.All){ 
                    OSXFileSystemWatcher watcher = new OSXFileSystemWatcher(repo.Path);
                    watcher.Changed += delegate(string path) {
                        Console.WriteLine(path);
                        if(Working) 
                        {
                            try{
                                Method (path);
                            }catch (DisconnectionException)
                            {
                                SyncStatus = SyncStatus.IDLE;
                                Program.Controller.HandleDisconnection();
                            }
                        }
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

        void Method (string path)
        {
            RepositoryItem item;
            LocalRepository repo = repositoryDAO.GetRepositoryByItemFullName (path);

            if (Directory.Exists(path)){
                item = RepositoryItem.CreateInstance (repo, path, true, 0, DateTime.Now.ToString ());
            }
            else if (File.Exists (path)){
                item = RepositoryItem.CreateInstance (repo, path, false, 0, DateTime.Now.ToString ());
            }else{
                item = RepositoryItem.CreateInstance (repo, path, false, 0, DateTime.Now.ToString ());
                item.IsAFolder = new SQLiteRepositoryItemDAO().IsFolder(item);
            }

            if(!item.IsIgnoreFile){
                Event e = new Event();
                e.Item = item;
                e.RepositoryType = RepositoryType.LOCAL;
                Synchronize(e);
                
                LastLocalEvent = e;
                LastTimeSync = DateTime.Now;
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
            foreach (OSXFileSystemWatcher watcher in watchers)
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

