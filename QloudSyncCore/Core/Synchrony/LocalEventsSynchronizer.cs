using System;
using GreenQloud.Repository;
using GreenQloud.Model;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using GreenQloud.Persistence.SQLite;

namespace GreenQloud.Synchrony
{
    public class LocalEventsSynchronizer : AbstractSynchronizer<LocalEventsSynchronizer>
    {
        private SQLiteRepositoryDAO repositoryDAO = new SQLiteRepositoryDAO();
        private SQLiteEventDAO eventDAO;
        private Thread watcherThread;
        private QloudSyncFileSystemWatcher watcher;
        public delegate void FinishedEventHandler ();
        object _lock = new object();

        public LocalEventsSynchronizer (LocalRepository repo, SynchronizerUnit unit) : base (repo, unit)
        {
            eventDAO = new SQLiteEventDAO(repo);
        }

        public override void Run(){
            if(watcherThread == null){
                watcherThread = new Thread(()=>{
                    watcher = new QloudSyncFileSystemWatcher(repo);
                    watcher.Changed += delegate(Event e) {
                        lock(_lock){
                            CreateEvent (e);
                            canChange = true;
                        }
                    };
                });
                watcherThread.Start ();
            }
            Thread.Sleep(1000);
        }

        public QloudSyncFileSystemWatcher GetWatcher(){
            return watcher;
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

