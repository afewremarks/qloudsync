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
    public abstract class BacklogSynchronizer : Synchronizer
    {
        TransferDAO transferDAO;
        LogicalRepositoryController logicalLocalRepository;
        PhysicalRepositoryController physicalLocalRepository;
        RemoteRepositoryController remoteRepository;

        protected BacklogSynchronizer (LogicalRepositoryController logicalLocalRepository, PhysicalRepositoryController physicalLocalRepository, RemoteRepositoryController remoteRepository, TransferDAO transferDAO)
        {
            Status = SyncStatus.IDLE;
            this.transferDAO = transferDAO;
            this.logicalLocalRepository = logicalLocalRepository;
            this.physicalLocalRepository = physicalLocalRepository;
            this.remoteRepository = remoteRepository;
        }

        public override Event GetEvent (RepositoryItem item, RepositoryType type){

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
            return e;
        }

        Event GetRemoteEvent (RepositoryItem item)
        {
            Event e = new Event ();
            e.Item = item;

            if (physicalLocalRepository.Exists (item)) {
                if (!item.IsSync) {
                    if (CalculateLastestVersion (item) == RepositoryType.LOCAL) {
                        e.EventType = EventType.UPDATE;
                        e.RepositoryType = RepositoryType.LOCAL;
                    }
                    else {
                        e.EventType = EventType.UPDATE;
                        e.RepositoryType = RepositoryType.REMOTE;
                    }
                }
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
            return e;
        }
        public override void Synchronize (){
            List<RepositoryItem> itensInRemoteRepository = remoteRepository.Files;
            List<RepositoryItem> filesInPhysicalLocalRepository = physicalLocalRepository.Files;
 
            foreach (RepositoryItem remoteItem in itensInRemoteRepository) {
                if (remoteItem.IsIgnoreFile)
                    continue;            
                Event e = GetEvent (remoteItem, RepositoryType.REMOTE);
                Synchronize (e);
                filesInPhysicalLocalRepository.RemoveAll (i=> i.FullLocalName == remoteItem.FullLocalName);
            }

            foreach (RepositoryItem localItem in filesInPhysicalLocalRepository)
            {
                Event e = GetEvent (localItem, RepositoryType.LOCAL);
                Synchronize (e);
            }

        }

        public override void Synchronize (Event e)
        {
            Transfer transfer = null;
            if (e.RepositoryType == RepositoryType.LOCAL){

                Status = SyncStatus.UPLOADING;

                if (e.EventType == EventType.DELETE)
                    transfer = remoteRepository.MoveFileToTrash (e.Item);
                else
                    transfer = remoteRepository.Upload (e.Item);

            }else{
                switch (e.EventType){
                case EventType.CREATE: 
                case EventType.UPDATE:
                    Status = SyncStatus.DOWNLOADING;
                    transfer = remoteRepository.Download (e.Item);
                    break;
                case EventType.DELETE:
                    Status = SyncStatus.UPLOADING;
                    transfer = remoteRepository.SendLocalVersionToTrash (e.Item);
                    physicalLocalRepository.Delete (e.Item);
                    break;
                }
            }
            
            if (transfer != null)
                transferDAO.Create (transfer);
            logicalLocalRepository.Solve (e.Item);
        }



        RepositoryType CalculateLastestVersion (RepositoryItem remoteObj)
        {
            TimeSpan diffClocks = remoteRepository.DiffClocks;

            RepositoryItem physicalObjectVersion = physicalLocalRepository.CreateObjectInstance (remoteObj.FullLocalName);
            
            DateTime referencialClock = physicalObjectVersion.TimeOfLastChange.Subtract (diffClocks);

            if (referencialClock.Subtract (Convert.ToDateTime (remoteObj.TimeOfLastChange)).TotalSeconds > -1) 
                return RepositoryType.LOCAL;

            return RepositoryType.REMOTE;
        }
    }
}


