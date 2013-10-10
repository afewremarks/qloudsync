using System;
using GreenQloud.Repository.Local;
using GreenQloud.Model;
using System.Threading;
using QloudSyncCore.Core.Persistence;
using System.Collections.Generic;
using System.IO;
using GreenQloud.Repository;

namespace GreenQloud.Synchrony
{
    public class LocalEventsSynchronizer : AbstractSynchronizer<LocalEventsSynchronizer>
    {
        private RepositoryRaven repositoryDAO = new RepositoryRaven();
        private EventRaven eventDAO;
        private Thread watcherThread;
        private QloudSyncFileSystemWatcher watcher;
        public delegate void FinishedEventHandler ();
        object _lock = new object();

        public LocalEventsSynchronizer (LocalRepository repo) : base (repo)
        {
            eventDAO = new EventRaven(repo);
        }

        public override void Run(){
            if(watcherThread == null){
                watcherThread = new Thread(()=>{
                    watcher = new QloudSyncFileSystemWatcher(repo);
                    watcher.Changed += delegate(Event e) {
                        lock(_lock){
                            CreateEvent (e);
                        }
                    };
                });
                watcherThread.Start ();
            }
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

