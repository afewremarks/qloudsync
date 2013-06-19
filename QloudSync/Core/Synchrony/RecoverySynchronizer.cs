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
            while (true) {
                Synchronize ();
            }
        }

        public void Synchronize (){
            eventDAO.RemoveAllUnsynchronized();

            List<RepositoryItem> localItems = localRepository.Items;
            List<RepositoryItem> remoteItems = remoteRepository.Items;
            SolveItems (localItems, remoteItems);

            Abort();//TODO remove this...
        }

        void SolveItems (List<RepositoryItem> localItems, List<RepositoryItem> remoteItems)
        {
            //items exists on remote...
            foreach (RepositoryItem item in remoteItems) {
                Event e = SolveFromRemote (item);
                localItems.RemoveAll( i => i.Key == item.Key);
                if (e != null) {
                    eventDAO.Create (e);
                }
            }

            //Items here is not on remote.... so, it only can be created or removed remote
            foreach (RepositoryItem item in localItems) {
                Event e = SolveFromLocal (item);
                if (e != null) {
                    eventDAO.Create (e);
                }
            }

            SynchronizerResolver.GetInstance ().SolveAll ();
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


