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
    public abstract class AbstractLocalEventsSynchronizer : AbstractSynchronizer
    {
        Thread threadSync;
        bool creatingEvent;

        protected AbstractLocalEventsSynchronizer 
            (LogicalRepositoryController logicalLocalRepository, PhysicalRepositoryController physicalLocalRepository, RemoteRepositoryController remoteRepository, TransferDAO transferDAO, EventDAO eventDAO) :
             base (logicalLocalRepository, physicalLocalRepository, remoteRepository, transferDAO, eventDAO)
        {
            threadSync = new Thread(() =>{
                Synchronize ();
            });
           
        }

        public void Synchronize (Event e){           
            eventDAO.SetEventType (GetEventType (e));
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



        public Event GetEventType (Event e)
        {
            bool exists = physicalLocalRepository.Exists (e.Item);
            if (exists){
                if (remoteRepository.ExistsCopies (e.Item)){
                       e.EventType = EventType.MOVE_OR_RENAME;
                }
                else{
                    if (remoteRepository.Exists (e.Item)){
                        e.EventType = EventType.UPDATE;
                    }
                    else{
                        e.EventType = EventType.CREATE;
                    }
                }
            }else{
                e.EventType = EventType.DELETE;
            }
            Console.WriteLine ("LocalEvents found an event: {0} {1} {2}", e.EventType, e.RepositoryType, e.Item.FullLocalName);

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

