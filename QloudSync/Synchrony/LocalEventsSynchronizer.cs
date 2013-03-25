using System;

using System.IO;
using System.Linq;
using System.Collections.Generic;
using GreenQloud.Synchrony;
using GreenQloud.Repository;
using GreenQloud.Util;
using System.Threading;
using GreenQloud.Model;
using GreenQloud.Repository.Local;
using GreenQloud.Repository.Remote;
using GreenQloud.Persistence;

namespace GreenQloud.Synchrony
{
    public abstract class LocalEventsSynchronizer : Synchronizer
    {
        Thread threadSync;
        bool creatingEvent;

        protected LocalEventsSynchronizer 
            (LogicalRepositoryController logicalLocalRepository, PhysicalRepositoryController physicalLocalRepository, RemoteRepositoryController remoteRepository, TransferDAO transferDAO, EventDAO eventDAO) :
             base (logicalLocalRepository, physicalLocalRepository, remoteRepository, transferDAO, eventDAO)
        {
            threadSync = new Thread(() =>{
                Synchronize ();
            });
           
        }

        public void Synchronize (RepositoryItem item){
            eventDAO.Create ( GetEvent (item));
            creatingEvent = true;
        }

        public new void Synchronize(){
            while (Working){
                if (creatingEvent){
                    creatingEvent = false;
                    if(SyncStatus == SyncStatus.IDLE){
                        base.Synchronize ();
                    }
                }
                Thread.Sleep (500);
            }
        }

        private bool Working {
            get;
            set;
        }

        public Event GetEvent (RepositoryItem item)
        {
            Event e = new Event();
            e.Item = item;
            e.RepositoryType = RepositoryType.LOCAL;
            e.Synchronized = false;

            if (physicalLocalRepository.Exists (item)){
                if (remoteRepository.ExistsCopys (item)){
                    e.EventType = EventType.MOVE_OR_RENAME;
                }
                else{
                    if (remoteRepository.Exists (item))
                        e.EventType = EventType.UPDATE;

                    else
                        e.EventType = EventType.CREATE;
                }
            }else
                e.EventType = EventType.DELETE;

            return e;
            
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

