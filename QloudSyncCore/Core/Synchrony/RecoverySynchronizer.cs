using System;
using System.Collections.Generic;
using GreenQloud.Repository;
using System.Linq;
using System.Xml;
using System.Threading;
using GreenQloud.Model;
using GreenQloud.Persistence.SQLite;
using System.IO;
using GreenQloud.Core;

 namespace GreenQloud.Synchrony
{
    public class RecoverySynchronizer : AbstractSynchronizer<RecoverySynchronizer>
    {
        private IRemoteRepositoryController remoteRepository;
        private IPhysicalRepositoryController localRepository;
        private SQLiteEventDAO eventDAO;
        private Dictionary<string, Thread> executingThreads;
        private Object lokkThreads = new object();
        private DateTime lastReleased;

        public DateTime LastReleased
        {
            get { return lastReleased; }
        }

        public RecoverySynchronizer(LocalRepository repo, SynchronizerUnit unit)
            : base(repo, unit)
        {
            remoteRepository = new RemoteRepositoryController (repo);
            localRepository = new PhysicalRepositoryController (repo);
            eventDAO = new SQLiteEventDAO(repo);
            this.executingThreads = new Dictionary<string, Thread>();
        }

        public override void Run() {
            lastReleased = GlobalDateTime.Now;
            while (!Stoped)
            {
                CheckRemoteFolder();
                Synchronize();
            }
        }

        void CheckRemoteFolder ()
        {
            if (repo.RemoteFolder.Length > 0)
            {
                Event e = new Event(repo);
                RepositoryItem item1 = RepositoryItem.CreateInstance(repo, repo.RemoteFolder);
                e.Item = item1;
                e.RepositoryType = RepositoryType.LOCAL;
                e.EventType = EventType.CREATE;
                eventDAO.Create(e);
                if (remoteRepository.Exists(item1))
                {
                    eventDAO.UpdateToSynchronized(e, RESPONSE.IGNORED);
                }
            }
        }

        private void SolveFromPrefix(string prefix) {
            if (!Stoped)
            {
                Thread t = new Thread(delegate()
                {
                    try
                    {
                        List<RepositoryItem> localItems = localRepository.GetItems(Path.Combine(repo.Path, prefix));
                        List<RepositoryItem> remoteItems = remoteRepository.GetItems(prefix);
                        SolveItems(localItems, remoteItems, prefix);
                    } catch (Exception e){
                        Program.GeneralUnhandledExceptionHandler(this, new UnhandledExceptionEventArgs(e, false)); 
                    }
                });
                lock (lokkThreads)
                {
                    if (!this.executingThreads.ContainsKey(prefix))
                    {
                        this.executingThreads.Add(prefix, t);
                        t.Start();
                    }
                }
            }
        }

        public void Synchronize (){
            string prefix = repo.RemoteFolder;
            SolveFromPrefix(repo.RemoteFolder);
            int count = 0;
            do {
                Wait(5000);
                lock (lokkThreads)
                {
                    count = executingThreads.Count;
                }
            } while (!Stoped &&  count > 0) ;
               
            if(!Stoped)
                canChange = true;

            lastReleased = GlobalDateTime.Now;

            //Only run one time...
           repo.Recovering = false;
           SQLiteRepositoryDAO dao = new SQLiteRepositoryDAO();
           dao.Update(repo);
            
            Stop();
            //Wait(60000);
        }

        void SolveItems (List<RepositoryItem> localItems, List<RepositoryItem> remoteItems, string prefix)
        {
            //items exists on remote...
            for (int i = 0; i < remoteItems.Count; i++) {
                RepositoryItem item1 = remoteItems [i];
                Event e = SolveFromRemote (item1);
                localItems.RemoveAll( it => it.Key == item1.Key);
                if (e != null) {
                    if ((e.EventType == EventType.DELETE || e.EventType == EventType.MOVE) && e.Item.IsFolder) {
                        localItems.RemoveAll( it => it.Key.StartsWith(item1.Key));
                        remoteItems.RemoveAll( it => it.Key.StartsWith(item1.Key));
                    }
                    if (RequestForChange())
                        eventDAO.Create(e);
                    
                }
                if (item1.IsFolder)
                    SolveFromPrefix(item1.Key);
            }

            //Items here is not on remote.... so, it only can be created or removed remote
            for (int i = 0; i < localItems.Count; i++) {
                RepositoryItem item2 = localItems [i];
                Event e = SolveFromLocal (item2);
                if (e != null) {
                    if ((e.EventType == EventType.DELETE || e.EventType == EventType.MOVE) && e.Item.IsFolder) {
                        localItems.RemoveAll( it => it.Key.StartsWith(item2.Key));
                        remoteItems.RemoveAll( it => it.Key.StartsWith(item2.Key));
                    }
                    if (RequestForChange())
                        eventDAO.Create(e);
                    
                }
                if (item2.IsFolder)
                    SolveFromPrefix(item2.Key);
            }
            lock (lokkThreads)
            {
                this.executingThreads.Remove(prefix);
            }
        }

        private bool RequestForChange()
        {
            if (!Stoped)
            {
                //Ask local if can make the change
                if (!unit.LocalEventsSynchronizer.IsStoped())
                {
                    unit.LocalEventsSynchronizer.WaitForChanges(2000);
                }
                if (!unit.RemoteEventsSynchronizer.IsStoped())
                {
                    unit.RemoteEventsSynchronizer.WaitForChanges(10000);
                }
                //Ask remote if can make the change
                return true;
            }

            return false;
        }


        private Event SolveFromRemote (RepositoryItem item)
        {
            if (eventDAO.ExistsAnyConflict(item.Key) || !remoteRepository.Exists(item))
            {
                return null;
            }

            Event e = new Event (repo);
            e.Item = item;
            if (localRepository.Exists (e.Item)) {
                string actualRemoteEtag = remoteRepository.RemoteETAG (e.Item);
                string actualLocalEtag = new Crypto().md5hash (e.Item);
                string savedEtag = e.Item.ETag;

                if (actualRemoteEtag != actualLocalEtag) {
                    if (savedEtag != actualRemoteEtag && savedEtag == actualLocalEtag) {//changed remote but still the same on local....
                        e.RepositoryType = RepositoryType.REMOTE;
                        e.EventType = EventType.UPDATE;
                    } else if (savedEtag == actualRemoteEtag && savedEtag != actualLocalEtag) {//changed local but still the same on remote....
                        e.RepositoryType = RepositoryType.LOCAL;
                        e.EventType = EventType.UPDATE;
                    } else {
                        Logger.LogInfo ("WARNING", "Recovery Synchronizer found both update local and remote on " + item.Key + " and cannot merge this."); //TODO MAKE A MANUAL MERGE DECISION
                        return null;
                    }
                    return e;
                }
            } else {
                if(e.Item.Id != 0 && (e.Item.UpdatedAt != null && e.Item.Moved == false)){
                    e.RepositoryType = RepositoryType.LOCAL;
                    e.EventType = EventType.DELETE;
                    return e;
                } else {
                    e.RepositoryType = RepositoryType.REMOTE;
                    e.EventType = EventType.CREATE;
                    return e;
                }
            }
            return null;
        }

        private Event SolveFromLocal (RepositoryItem item)
        {
            if (eventDAO.ExistsAnyConflict(item.Key) || !localRepository.Exists(item))
            {
                return null;
            }


            Event e = new Event (repo);
            e.Item = item;
            if (localRepository.Exists (e.Item)) {
                if (e.Item.UpdatedAt == null) {
                    e.RepositoryType = RepositoryType.LOCAL;
                    e.EventType = EventType.CREATE;
                    return e;
                } else if (!remoteRepository.Exists(e.Item)) {
                    e.RepositoryType = RepositoryType.REMOTE;
                    e.EventType = EventType.DELETE;
                    return e;
                }
            }
            return null;
        }
    }
}


