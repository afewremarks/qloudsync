using Amazon.S3.Model;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Threading;

using GreenQloud.Model;
using GreenQloud.Repository;
using GreenQloud.Util;
using GreenQloud.Persistence;
using GreenQloud.Repository.Local;
using GreenQloud.Repository.Remote;

namespace GreenQloud.Synchrony
{
    
    public enum SyncStatus{
        IDLE,
        UPLOADING,
        DOWNLOADING,
        VERIFING
    }

    public abstract class Synchronizer
    {
        protected delegate void ProgressChangedEventHandler (double percentage, double time);

        protected TransferDAO transferDAO;
        protected EventDAO eventDAO;
        protected LogicalRepositoryController logicalLocalRepository;
        protected PhysicalRepositoryController physicalLocalRepository;
        protected RemoteRepositoryController remoteRepository;
        
        protected Synchronizer 
            (LogicalRepositoryController logicalLocalRepository, PhysicalRepositoryController physicalLocalRepository, 
             RemoteRepositoryController remoteRepository, TransferDAO transferDAO, EventDAO eventDAO)
        {
            SyncStatus = SyncStatus.IDLE;
            this.transferDAO = transferDAO;
            this.eventDAO = eventDAO;
            this.logicalLocalRepository = logicalLocalRepository;
            this.physicalLocalRepository = physicalLocalRepository;
            this.remoteRepository = remoteRepository;
        }


        
        public void Synchronize(){
            List<Event> eventsNotSynchronized = eventDAO.EventsNotSynchronized;
            while (eventsNotSynchronized.Count>0){
                Synchronize (eventsNotSynchronized[0]);
                eventsNotSynchronized = eventDAO.EventsNotSynchronized;
            }
        }

        void Synchronize(Event e){
            Transfer transfer = null;

            if (e.RepositoryType == RepositoryType.LOCAL){

                SyncStatus = SyncStatus.UPLOADING;

                if (e.EventType == EventType.DELETE)
                    transfer = remoteRepository.MoveFileToTrash (e.Item);
                else
                    transfer = remoteRepository.Upload (e.Item);
                
            }else{

                switch (e.EventType){
                case EventType.CREATE: 
                case EventType.UPDATE:
                    SyncStatus = SyncStatus.DOWNLOADING;
                    transfer = remoteRepository.Download (e.Item);
                    break;
                case EventType.DELETE:
                    SyncStatus = SyncStatus.UPLOADING;
                    transfer = remoteRepository.SendLocalVersionToTrash (e.Item);
                    physicalLocalRepository.Delete (e.Item);
                    break;
                }

            }
            
            if (transfer != null)
                transferDAO.Create (transfer);
            logicalLocalRepository.Solve (e.Item);
            eventDAO.UpdateToSynchronized(e);
        }

        #region Abstract Methods

        public abstract void Start ();
        public abstract void Pause ();
        public abstract void Stop ();

        #endregion

        #region Implemented Methods

        private SyncStatus status;
        public delegate void SyncStatusChangedHandler (SyncStatus status);
        public event SyncStatusChangedHandler SyncStatusChanged = delegate {};

        public SyncStatus SyncStatus {
            get {
                return status;
            }
            set {
                status = value;
                SyncStatusChanged(status);
            }
        }

        public bool Done {
            set; get;
        }

        int countOperation = 0;

        protected void ShowDoneMessage (string action)
        {
            if (countOperation == 0)
                Logger.LogInfo (action, "Files up to date.\n");
            else
                Logger.LogInfo(action, string.Format("Successful: {0} files.\n",countOperation));
        }   
        #endregion
    }
}