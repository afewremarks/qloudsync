using System;
using GreenQloud.Synchrony;
using System.Threading;
using GreenQloud.Model;

namespace GreenQloud.Repository
{
    public abstract class AbstractController
    {
        protected LocalRepository repo;

        public AbstractController(LocalRepository repo){
            this.repo = repo;
        }

        protected void BlockWatcher (string path)
        {
            QloudSyncFileSystemWatcher watcher = SynchronizerUnit.GetByRepo(repo).LocalEventsSynchronizer.GetWatcher();
            if(watcher != null){
                watcher.Block (path);
            }

        }

        protected void UnblockWatcher (string path)
        {
            QloudSyncFileSystemWatcher watcher = SynchronizerUnit.GetByRepo(repo).LocalEventsSynchronizer.GetWatcher();
            if (watcher != null) {
                Thread.Sleep (2000);
                watcher.Unblock (path);
            }

        }

    }
}

