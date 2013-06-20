using System;
using System.Collections.Generic;
using GreenQloud.Repository;
using System.Linq;
using System.Xml;
using System.Threading;
using GreenQloud.Model;
using GreenQloud.Persistence;
using GreenQloud.Repository.Local;
using GreenQloud.Persistence.SQLite;

 namespace GreenQloud.Synchrony
{
    public class RecoverySynchronizer : AbstractSynchronizer<RecoverySynchronizer>
    {
        private IRemoteRepositoryController remoteRepository = new RemoteRepositoryController ();
        private IPhysicalRepositoryController localRepository = new StorageQloudPhysicalRepositoryController ();
        private SQLiteEventDAO eventDAO = new SQLiteEventDAO();

        public RecoverySynchronizer () : base () { }

        public override void Run() {
            Synchronize ();
        }

        private bool startedSync = false;
        public bool StartedSync{
            get {
                return startedSync;
            }
        }
        public void Synchronize (){
            eventDAO.RemoveAllUnsynchronized();
            startedSync = true; //only after remove all unsync events.

            List<RepositoryItem> localItems = localRepository.Items;
            List<RepositoryItem> remoteItems = remoteRepository.Items;
            SolveItems (localItems, remoteItems);
        }

        void SolveItems (List<RepositoryItem> localItems, List<RepositoryItem> remoteItems)
        {
            //items exists on remote...
            for (int i = 0; i < remoteItems.Count; i++) {
                RepositoryItem item = remoteItems [i];
                Event e = SolveFromRemote (item);
                localItems.RemoveAll( it => it.Key == item.Key);
                if (e != null) {
                    if ((e.EventType == EventType.DELETE || e.EventType == EventType.MOVE) && e.Item.IsFolder) {
                        localItems.RemoveAll( it => it.Key.StartsWith(item.Key));
                        remoteItems.RemoveAll( it => it.Key.StartsWith(item.Key));
                    }
                    eventDAO.Create (e);
                }
            }

            //Items here is not on remote.... so, it only can be created or removed remote
            for (int i = 0; i < localItems.Count; i++) {
                RepositoryItem item = localItems [i];
                Event e = SolveFromLocal (item);
                if (e != null) {
                    if ((e.EventType == EventType.DELETE || e.EventType == EventType.MOVE) && e.Item.IsFolder) {
                        localItems.RemoveAll( it => it.Key.StartsWith(item.Key));
                        remoteItems.RemoveAll( it => it.Key.StartsWith(item.Key));
                    }
                    eventDAO.Create (e);
                }
            }
        }


        private Event SolveFromRemote (RepositoryItem item)
        {
            Event e = new Event ();
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
                if(e.Item.Id != 0 && e.Item.Moved == false){
                    e.RepositoryType = RepositoryType.LOCAL;
                    e.EventType = EventType.DELETE;
                } else {
                    e.RepositoryType = RepositoryType.REMOTE;
                    e.EventType = EventType.CREATE;
                }
                return e;
            }
            return null;
        }

        private Event SolveFromLocal (RepositoryItem item)
        {
            Event e = new Event ();
            e.Item = item;
            if (localRepository.Exists (e.Item)) {
                if (e.Item.Id == 0) {
                    e.RepositoryType = RepositoryType.LOCAL;
                    e.EventType = EventType.CREATE;
                } else {
                    e.RepositoryType = RepositoryType.REMOTE;
                    e.EventType = EventType.DELETE;
                }
                return e;
            }
            return null;
        }
    }
}


