using System;
using GreenQloud.Synchrony;
using System.Threading;

namespace GreenQloud.Repository
{
    public class AbstractController
    {

            protected static void BlockWatcher (string path)
            {
                QloudSyncFileSystemWatcher watcher = StorageQloudLocalEventsSynchronizer.GetInstance ().GetWatcher (path);
                if(watcher != null){
                    watcher.Block (path);
                }

            }

            protected static void UnblockWatcher (string path)
            {
                QloudSyncFileSystemWatcher watcher = StorageQloudLocalEventsSynchronizer.GetInstance ().GetWatcher (path);
                if (watcher != null) {
                    Thread.Sleep (1000);
                    watcher.Unblock (path);
                }

            }

    }
}

