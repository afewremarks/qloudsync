using System;
using GreenQloud.Model;
using GreenQloud.Repository.Local;
using GreenQloud.Repository.Remote;
using GreenQloud.Persistence;
using System.Collections.Generic;
using System.Threading;

namespace GreenQloud.Synchrony
{
    public class RemoteEventsSynchronizer : Synchronizer
    {
        Thread threadSync;
        bool eventsCreated;

        public RemoteEventsSynchronizer  
            (LogicalRepositoryController logicalLocalRepository, PhysicalRepositoryController physicalLocalRepository, RemoteRepositoryController remoteRepository, TransferDAO transferDAO, EventDAO eventDAO) :
            base (logicalLocalRepository, physicalLocalRepository, remoteRepository, transferDAO, eventDAO)
        {
            threadSync = new Thread(() =>{
                Synchronize ();
            });
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

        public void AddEvents ()
        {

            foreach (RepositoryItem remoteItem in remoteRepository.RecentChangedItems){

                if (physicalLocalRepository.Exists (remoteItem)){
                    if (!remoteItem.IsSync)
                    {
                        eventDAO.Create ( new Event(){
                            EventType = EventType.UPDATE,
                            RepositoryType = RepositoryType.REMOTE,
                            Item = remoteItem,
                            Synchronized = false
                        });
                    }
                }
                else {
                    RepositoryItem copy = physicalLocalRepository.GetCopy (remoteItem);
                    if (copy != null){
                        if (!remoteRepository.Exists (copy)){
                            eventDAO.Create ( new Event(){
                                EventType = EventType.MOVE_OR_RENAME,
                                RepositoryType = RepositoryType.REMOTE,
                                Item = remoteItem,
                                Synchronized = false
                            });
                        }
                        else{
                            eventDAO.Create ( new Event(){
                                EventType = EventType.COPY,
                                RepositoryType = RepositoryType.REMOTE,
                                Item = remoteItem,
                                Synchronized = false
                            });
                        }
                    }
                    else{
                        eventDAO.Create ( new Event(){
                            EventType = EventType.CREATE,
                            RepositoryType = RepositoryType.REMOTE,
                            Item = remoteItem,
                            Synchronized = false
                        });
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
                    eventDAO.Create (e);
                }

            }  
            eventsCreated = true;
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
    }
}

