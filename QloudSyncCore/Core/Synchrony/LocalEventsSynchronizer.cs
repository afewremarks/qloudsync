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
        public delegate void FinishedEventHandler ();
        object _lock = new object();

        public LocalEventsSynchronizer () : base ()
        {
        }

        public override void Run(){
            if(watcherThread == null){
                watcherThread = new Thread(()=>{
                    watchers = new Dictionary<string, QloudSyncFileSystemWatcher>();
                    foreach (LocalRepository repo in repositoryDAO.All){ 
                        QloudSyncFileSystemWatcher watcher = new QloudSyncFileSystemWatcher(repo.Path);
                        watcher.Changed += delegate(Event e) {
                            lock(_lock){
                                CreateEvent (e);
                            }
                        };
                        watchers.Add (repo.Path, watcher);
                    }
                });
                watcherThread.Start ();
            }
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
            if(!_stoped )
                Create(e);
        }

        public void Create (Event e){       
            e.RepositoryType = RepositoryType.LOCAL;
            eventDAO.Create (e);
        }
    }
}

