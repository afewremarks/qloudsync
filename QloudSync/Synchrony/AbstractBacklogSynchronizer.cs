using System;
using System.Collections.Generic;
using GreenQloud.Repository;
using System.Linq;
using System.Xml;
using System.Threading;
using GreenQloud.Repository.Remote;
using GreenQloud.Model;
using GreenQloud.Persistence;
using GreenQloud.Repository.Local;

 namespace GreenQloud.Synchrony
{
    public abstract class AbstractBacklogSynchronizer : AbstractSynchronizer
    {

        protected AbstractBacklogSynchronizer 
            (LogicalRepositoryController logicalLocalRepository, PhysicalRepositoryController physicalLocalRepository, 
             RemoteRepositoryController remoteRepository, TransferDAO transferDAO, EventDAO eventDAO) :
            base (logicalLocalRepository, physicalLocalRepository, remoteRepository, transferDAO, eventDAO)
        {

        }

        public new void Synchronize (){
            List<RepositoryItem> itensInRemoteRepository = remoteRepository.Items;
            List<RepositoryItem> filesInPhysicalLocalRepository = physicalLocalRepository.Items;
            
            foreach (RepositoryItem remoteItem in itensInRemoteRepository) {
               
                if (remoteItem.IsIgnoreFile)
                    continue; 
                eventDAO.Create (GetEvent (remoteItem, RepositoryType.REMOTE));
                
                filesInPhysicalLocalRepository.RemoveAll (i=> i.FullLocalName == remoteItem.FullLocalName);
            }
            
            foreach (RepositoryItem localItem in filesInPhysicalLocalRepository)
            {
                eventDAO.Create (GetEvent (localItem, RepositoryType.LOCAL));
                
            }
            base.Synchronize();
        }

        public Event GetEvent (RepositoryItem item, RepositoryType type){

            if (type == RepositoryType.LOCAL)
                return GetLocalEvent (item);
            else
                return GetRemoteEvent (item);
        }

        Event GetLocalEvent (RepositoryItem item)
        {
            Event e = new Event ();
            e.Item = item;
           
            if (logicalLocalRepository.Exists (item)){
                e.EventType = EventType.DELETE;
                e.RepositoryType = RepositoryType.REMOTE;
            }else{
                e.EventType = EventType.CREATE;
                e.RepositoryType = RepositoryType.LOCAL;
            }
            Console.WriteLine ("Backlog found an event: {0} {1} {2}", e.EventType, e.RepositoryType, e.Item.FullLocalName);
            return e;
        }

        Event GetRemoteEvent (RepositoryItem item)
        {
            Event e = new Event ();
            e.Item = item; 

            if (physicalLocalRepository.Exists (item)) {
                if (!physicalLocalRepository.IsSync(item)) {
                    if (CalculateLastestVersion (item) == RepositoryType.LOCAL) {
                        e.EventType = EventType.UPDATE;
                        e.RepositoryType = RepositoryType.LOCAL;
                    }
                    else {
                        e.EventType = EventType.UPDATE;
                        e.RepositoryType = RepositoryType.REMOTE;
                    }
                }
                return null;
            }
            else{               
                if (logicalLocalRepository.Exists (item)){
                    e.EventType = EventType.DELETE;
                    e.RepositoryType = RepositoryType.LOCAL;
                }
                else{
                    e.EventType = EventType.CREATE;
                    e.RepositoryType = RepositoryType.REMOTE;
                }
            }
            Console.WriteLine ("Backlog found an event: {0} {1} {2}", e.EventType, e.RepositoryType, e.Item.FullLocalName);
            return e;
        }

        RepositoryType CalculateLastestVersion (RepositoryItem remoteObj)
        {
            TimeSpan diffClocks = remoteRepository.DiffClocks;

            RepositoryItem physicalObjectVersion = physicalLocalRepository.CreateItemInstance (remoteObj.FullLocalName);
            
            DateTime referencialClock = physicalObjectVersion.TimeOfLastChange.Subtract (diffClocks);

            if (referencialClock.Subtract (Convert.ToDateTime (remoteObj.TimeOfLastChange)).TotalSeconds > -1) 
                return RepositoryType.LOCAL;

            return RepositoryType.REMOTE;
        }
    }
}

