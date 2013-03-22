using System;
using System.Collections.Generic;
using GreenQloud.Repository;
using System.Linq;
using System.Xml;
using System.Threading;
using GreenQloud.Repository.Remote;
using GreenQloud.Repository.Model;
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

        private enum LATEST_VERSION{
            LOCAL,
            REMOTE
        }

        //TODO TRATAR DESCONEXOES E ERROS
        public override void Synchronize ( ){
           
            TransferResponse transfer;
            List<RepoObject> objectsInRemoteRepo = remoteRepository.Files;
            List<string> filesInPhysicalLocalRepository = physicalLocalRepository.FilesNames;

            foreach (RepoObject remoteObj in objectsInRemoteRepo) {
                if (remoteObj.IsIgnoreFile)
                    continue;

                transfer = null;
                if (physicalLocalRepository.Exists (remoteObj)) {
                    if (!remoteObj.IsSync) {
                        if (CalculateLastestVersion (remoteObj) == LATEST_VERSION.LOCAL) {
                             //TODO a decisao se eh pasta ou arquivo fica para ai
                            Status = SyncStatus.UPLOADING;
                            transfer = remoteRepository.Upload (remoteObj);
                        }
                        else {
                            Status = SyncStatus.DOWNLOADING;
                            transfer = remoteRepository.Download (remoteObj);
                        }
                    }
                }
                else{
                    //exists in backlog
                    if (logicalLocalRepository.Exists (remoteObj)){
                        // was delete locally when offline (after disconnection)
                        Status = SyncStatus.UPLOADING;
                        transfer = remoteRepository.MoveFileToTrash (remoteObj);
                    }
                    else{
                        // was created remotelly when client offline
                        Status = SyncStatus.DOWNLOADING;
                        transfer = remoteRepository.Download (remoteObj);
                    }
                }

                if (transfer != null)                
                    transferDAO.Create (transfer);
                logicalLocalRepository.Solve (remoteObj);
                filesInPhysicalLocalRepository.Remove (remoteObj.FullLocalName); 
            }

            foreach (string localObjectFullPath in filesInPhysicalLocalRepository)
            {

                RepoObject repoObj = logicalLocalRepository.CreateObjectInstance (localObjectFullPath);
                transfer = null;

                if (logicalLocalRepository.Exists (repoObj)){
                    Status = SyncStatus.UPLOADING;
                    remoteRepository.SendLocalVersionToTrash (repoObj);
                    physicalLocalRepository.Delete (repoObj);
                }else{
                    Status = SyncStatus.UPLOADING;
                    remoteRepository.Upload (repoObj);
                }

                if (transfer != null)
                    transferDAO.Create (transfer);
                logicalLocalRepository.Solve (repoObj);
            }

            Status = SyncStatus.IDLE;
        }

        LATEST_VERSION CalculateLastestVersion (RepoObject remoteObj)
        {
            TimeSpan diffClocks = remoteRepository.DiffClocks;

            RepoObject physicalObjectVersion = physicalLocalRepository.CreateObjectInstance (remoteObj.FullLocalName);
            
            DateTime referencialClock = physicalObjectVersion.TimeOfLastChange.Subtract (diffClocks);

            if (referencialClock.Subtract (Convert.ToDateTime (remoteObj.TimeOfLastChange)).TotalSeconds > -1) 
                return LATEST_VERSION.LOCAL;

            return LATEST_VERSION.REMOTE;
        }
    }
}


