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
using GreenQloud.Persistence.SQLite;

namespace GreenQloud.Synchrony
{
    
    public enum SyncStatus{
        IDLE,
        UPLOADING,
        DOWNLOADING,
        VERIFING
    }

    public class SynchronizerResolver
    {
        private static SynchronizerResolver instance;
        private SyncStatus status;
        private Thread threadSync;

        protected TransferDAO transferDAO;
        protected EventDAO eventDAO;
        protected LogicalRepositoryController logicalLocalRepository;
        protected PhysicalRepositoryController physicalLocalRepository;
        protected RemoteRepositoryController remoteRepository;

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

        public bool Working{
            set; get;
        }
        
        private SynchronizerResolver 
            (LogicalRepositoryController logicalLocalRepository, PhysicalRepositoryController physicalLocalRepository, 
             RemoteRepositoryController remoteRepository, TransferDAO transferDAO, EventDAO eventDAO)
        {
            SyncStatus = SyncStatus.IDLE;
            this.transferDAO = transferDAO;
            this.eventDAO = eventDAO;
            this.logicalLocalRepository = logicalLocalRepository;
            this.physicalLocalRepository = physicalLocalRepository;
            this.remoteRepository = remoteRepository;
            threadSync = new Thread(() =>{
                Synchronize ();
            });
        }

        public static SynchronizerResolver GetInstance(){
            if (instance == null)
                instance = new SynchronizerResolver (new StorageQloudLogicalRepositoryController(), 
                                                                new StorageQloudPhysicalRepositoryController(),
                                                                new StorageQloudRemoteRepositoryController(),
                                                                new SQLiteTransferDAO (),
                                                                new SQLiteEventDAO ());
            return instance;
        }

        public void Start ()
        {
            Working = true;
            try{
                threadSync.Start();
            }catch{
                Logger.LogInfo("ERROR", "Cannot start Synchronizer Resolver Thread");
            }
        }
        public void Pause ()
        {
            Working = false;
        }

        public void Stop ()
        {
            Working = false;
            threadSync.Join();
        }

        public void Synchronize(){//TODO REFATORAR!!!!!!
            while (Working){

                List<Event> eventsNotSynchronized = eventDAO.EventsNotSynchronized;
                while (eventsNotSynchronized.Count>0 && Working){
                    Synchronize (eventsNotSynchronized[0]);
                    eventsNotSynchronized = eventDAO.EventsNotSynchronized;
                }
                SyncStatus = SyncStatus.IDLE;
                Done = true;

                Thread.Sleep (1000);

            }

        }


        void Synchronize(Event e){
            try{
                Console.WriteLine ("\nSynchronizing: {0} {1} {2}\n",e.EventType, e.RepositoryType, e.Item.FullLocalName);

                Transfer transfer = null;
                if (e.RepositoryType == RepositoryType.LOCAL){
                    
                    SyncStatus = SyncStatus.UPLOADING;
                    
                    if (e.EventType == EventType.DELETE) {
                        transfer = remoteRepository.MoveToTrash (e.Item);
                        e.ResultObject =  e.Item.TrashFullName;
                        eventDAO.UpdateResultObject (e);
                    }
                    else
                        transfer = remoteRepository.Upload (e.Item);
                    
                }else{
                    switch (e.EventType){
                    case EventType.MOVE:
                        physicalLocalRepository.Move(e.Item, e.ResultObject);
                        break;
                    case EventType.CREATE: 
                    case EventType.UPDATE:
                    case EventType.COPY:
                        SyncStatus = SyncStatus.DOWNLOADING;
                        transfer = remoteRepository.Download (e.Item);
                        break;
                    case EventType.DELETE:
                        SyncStatus = SyncStatus.DOWNLOADING;
                        physicalLocalRepository.Delete (e.Item);
                        break;
                    }                
                }
                
                if (transfer != null)
                    transferDAO.Create (transfer);
                logicalLocalRepository.Solve (e.Item);
                eventDAO.UpdateToSynchronized(e);

                if(e.RepositoryType == RepositoryType.LOCAL){
                    new JSONHelper().postJSON (e);
                }
            } catch (Exception exc){
                //TODO refactor to catch error and treat
                Logger.LogInfo("ERROR", exc);
            }
        }
    }
}