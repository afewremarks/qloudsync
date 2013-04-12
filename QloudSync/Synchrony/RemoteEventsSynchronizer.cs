using System;
using GreenQloud.Model;
using GreenQloud.Repository.Local;
using GreenQloud.Repository.Remote;
using GreenQloud.Persistence;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace GreenQloud.Synchrony
{
    public class RemoteEventsSynchronizer : AbstractSynchronizer
    {
        Thread threadSync;
        bool eventsCreated;

        DateTime LastSyncTime;

        public RemoteEventsSynchronizer  
            (LogicalRepositoryController logicalLocalRepository, PhysicalRepositoryController physicalLocalRepository, RemoteRepositoryController remoteRepository, TransferDAO transferDAO, EventDAO eventDAO) :
            base (logicalLocalRepository, physicalLocalRepository, remoteRepository, transferDAO, eventDAO)
        {
            threadSync = new Thread(() =>{
                Synchronize ();
            });
            LastSyncTime = new DateTime();
        }



        public new void Synchronize(){
            while (Working){
                if (eventsCreated){
                    eventsCreated = false;
                    if(SyncStatus == SyncStatus.IDLE){
                        base.Synchronize ();
                    }
                }
                Thread.Sleep (500);
            }
        }

        bool ready=true;

        public void AddEvents ()
        {

            if (!ready)
                return;
            ready = false;
            if (LastSyncTime == new DateTime())
                LastSyncTime = DateTime.Now.Subtract(new TimeSpan (0,0,30));
            DateTime time = LastSyncTime;
            LastSyncTime = DateTime.Now;

            foreach (RepositoryItem remoteItem in remoteRepository.RecentChangedItems (time)){


                    bool exists = physicalLocalRepository.Exists (remoteItem);
                    if (exists){
                        if (!physicalLocalRepository.IsSync (remoteItem))
                        {
                            Event e = new Event(){
                                EventType = EventType.UPDATE,
                                RepositoryType = RepositoryType.REMOTE,
                                Item = remoteItem,
                                Synchronized = false
                            };
                            Logger.LogInfo ("Synchronizer", string.Format("RemoteEvents found an event: {0} {1} {2}", e.EventType, e.RepositoryType, e.Item.FullLocalName));
                            eventDAO.Create (e);
                        }
                    }
                    else {
                        RepositoryItem copy = physicalLocalRepository.GetCopy (remoteItem);
                        if (copy != null){
                            if (!remoteRepository.Exists (copy)){
                                Event e = new Event(){
                                    EventType = EventType.MOVE_OR_RENAME,
                                    RepositoryType = RepositoryType.REMOTE,
                                    Item = remoteItem,
                                    Synchronized = false
                                };
                                Logger.LogInfo ("Synchronizer", string.Format("RemoteEvents found an event: {0} {1} {2}", e.EventType, e.RepositoryType, e.Item.FullLocalName));
                                eventDAO.Create (e);
                            }
                            else{
                                Event e = new Event(){
                                    EventType = EventType.COPY,
                                    RepositoryType = RepositoryType.REMOTE,
                                    Item = remoteItem,
                                    Synchronized = false
                                };
                                Logger.LogInfo ("Synchronizer", string.Format("RemoteEvents found an event: {0} {1} {2}", e.EventType, e.RepositoryType, e.Item.FullLocalName));
                                eventDAO.Create (e);
                            }
                        }
                        else{
                            Event e = new Event(){
                                EventType = EventType.CREATE,
                                RepositoryType = RepositoryType.REMOTE,
                                Item = remoteItem,
                                Synchronized = false
                            };
                            Logger.LogInfo ("Synchronizer", string.Format("RemoteEvents found an event: {0} {1} {2}", e.EventType, e.RepositoryType, e.Item.FullLocalName));
                            eventDAO.Create (e);
                        }
                    }
                }
           foreach (RepositoryItem localItem in physicalLocalRepository.Items) {

                if (!remoteRepository.Exists(localItem)){
                    Event e = new Event ();

                    e.Item = localItem;
                    e.EventType = EventType.DELETE;
                    e.RepositoryType = RepositoryType.REMOTE;
                    e.Synchronized = false;
                    Logger.LogInfo ("Synchronizer", string.Format("RemoteEvents found an event: {0} {1} {2}", e.EventType, e.RepositoryType, e.Item.FullLocalName));
                    eventDAO.Create (e);
                }
            }  
            eventsCreated = true;

            ready = true;
        }

        public double InitFirstLoad ()
        {
            double size = 0;
            Start();
           
            foreach (RepositoryItem i in remoteRepository.Items){
                size+= i.Size;

                eventDAO.Create ( new Event(){
                    EventType = EventType.CREATE,
                    RepositoryType = RepositoryType.REMOTE,
                    Item = i,
                    Synchronized = false
                });
            }
            eventsCreated = true;
            return size;
        }


        #region implemented abstract members of Synchronizer
        public override void Start ()
        {
            Working = true;
            try{
                threadSync.Start();
            }catch{
                // do nothing
            }
        }
        public override void Pause ()
        {
            Working = false;
        }
        
        public override void Stop ()
        {
            Working = false;
            threadSync.Join();
        }
        #endregion  

        public void GenericSynchronize(){
            base.Synchronize();
        }
    }
}

