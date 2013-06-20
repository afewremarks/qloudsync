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
    public class LocalEventsSynchronizer : AbstractSynchronizer<LocalEventsSynchronizer>
    {
        private Dictionary<string, QloudSyncFileSystemWatcher> watchers;
        private SQLiteRepositoryDAO repositoryDAO = new SQLiteRepositoryDAO();
        private SQLiteEventDAO eventDAO = new SQLiteEventDAO();
        private Thread watcherThread;

        
 
        public event Action Failed = delegate { };
        public event FinishedEventHandler Finished = delegate { };
        public delegate void FinishedEventHandler ();
        private Event LastLocalEvent = new Event();
        private DateTime LastTimeSync = new DateTime();

        public LocalEventsSynchronizer () : base ()
        {
            watcherThread = new Thread(()=>{
                watchers = new Dictionary<string, QloudSyncFileSystemWatcher>();
                foreach (LocalRepository repo in repositoryDAO.All){ 
                    QloudSyncFileSystemWatcher watcher = new QloudSyncFileSystemWatcher(repo.Path);
                    watcher.Changed += delegate(Event e) {
                        Logger.LogEvent("EVENT FOUND", e);
                        CreateEvent (e);
                    };
                    watchers.Add (repo.Path, watcher);
                }
            });
        }

        public override void Run(){
            watcherThread.Start ();
        }

        public QloudSyncFileSystemWatcher GetWatcher(string path){
            if(watchers != null){
                QloudSyncFileSystemWatcher watcher = null;
                foreach(string watcherKey in watchers.Keys){
                    if (path.StartsWith (watcherKey)) {
                        watchers.TryGetValue (watcherKey, out watcher);
                        return watcher;
                    }
                }
                return watcher;
            }
            return null;
        }

        public void CreateEvent (Event e)
        {
            Create(e);

            LastLocalEvent = e;
            LastTimeSync = GlobalDateTime.Now;
        }

        public void Create (Event e){       
            e.RepositoryType = RepositoryType.LOCAL;
            eventDAO.Create (e);
        }
    }
}

