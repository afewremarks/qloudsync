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

        public void Synchronize (RepositoryItem item){           
            eventDAO.Create (GetEvent (item));
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

        public Event GetEvent (RepositoryItem item)
        {
            Event e = new Event();
            e.Item = item;
            e.RepositoryType = RepositoryType.LOCAL;
            e.Synchronized = false;
            Console.WriteLine (item.FullLocalName);
            if (physicalLocalRepository.Exists (item)){
                if (remoteRepository.ExistsCopies (item)){                                       
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

