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

        protected EventDAO eventDAO;
        protected RepositoryItemDAO repositoryItemDAO;
        protected LogicalRepositoryController logicalLocalRepository;
        protected IPhysicalRepositoryController physicalLocalRepository;
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

        private int eventsToSync;
        public int EventsToSync{
            get { return eventsToSync; }
        }
        public bool Done {
            set; get;
        }

        public bool Working{
            set; get;
        }
        
        private SynchronizerResolver 
            (LogicalRepositoryController logicalLocalRepository, IPhysicalRepositoryController physicalLocalRepository, 
             RemoteRepositoryController remoteRepository, EventDAO eventDAO, RepositoryItemDAO repositoryItemDAO)
        {
            SyncStatus = SyncStatus.IDLE;
            this.eventDAO = eventDAO;
            this.repositoryItemDAO = repositoryItemDAO;
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
                                                                new RemoteRepositoryController(),
                                                                new SQLiteEventDAO (),
                                                                new SQLiteRepositoryItemDAO());
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

        public void Synchronize(){//TODO REFACTOR!!!!!!
            while (Working){

                List<Event> eventsNotSynchronized = eventDAO.EventsNotSynchronized;

                eventsToSync = eventsNotSynchronized.Count;
                while (eventsNotSynchronized.Count>0 && Working){
                    Synchronize (eventsNotSynchronized[0]);
                    eventsNotSynchronized = eventDAO.EventsNotSynchronized;
                }
                SyncStatus = SyncStatus.IDLE;
                Done = true;

                Thread.Sleep (1000);

            }

        }


        //TODO refactor ignores
        private bool VerifyIgnoreRemote (Event remoteEvent)
        {
            return false;
        }
        private bool VerifyIgnoreLocal (Event remoteEvent)
        {
            if(remoteEvent.Item.ResultItem == null){
                if (!remoteEvent.Item.IsFolder) {
                    FileInfo fi = new FileInfo (remoteEvent.Item.LocalAbsolutePath);
                    if (fi.Length == 0)
                        return true;
                }
            }
            return false;
        }

        private bool VerifyIgnore (Event e)
        {
            if(e.RepositoryType == RepositoryType.REMOTE)
               return VerifyIgnoreRemote (e);
            if(e.RepositoryType == RepositoryType.LOCAL)
                return VerifyIgnoreLocal (e);

            return false;
        }

        void Synchronize(Event e){
            if (VerifyIgnore (e)) {
                eventDAO.UpdateToSynchronized(e);
                Logger.LogInfo ("EVENT IGNORE", "Ignore event on " + e.Item.LocalAbsolutePath);
                return;
            }

            try{
                Logger.LogEvent("Event Synchronizing", e);
                if (e.RepositoryType == RepositoryType.LOCAL){
                    
                    SyncStatus = SyncStatus.UPLOADING;

                    switch (e.EventType){
                        case EventType.CREATE: 
                        case EventType.UPDATE:
                            remoteRepository.Upload (e.Item);
                            break;
                        case EventType.DELETE:
                            remoteRepository.Delete (e.Item);
                            break;
                        case EventType.COPY:
                            remoteRepository.Copy (e.Item);
                            break;
                        case EventType.MOVE:
                            remoteRepository.Move (e.Item);
                            break;
                    }
                }else{
                    switch (e.EventType){
                    case EventType.MOVE:
                        physicalLocalRepository.Move(e.Item);
                        break;
                    case EventType.CREATE: 
                    case EventType.UPDATE:
                        SyncStatus = SyncStatus.DOWNLOADING;
                        remoteRepository.Download (e.Item);
                        break;
                    case EventType.COPY:
                        SyncStatus = SyncStatus.DOWNLOADING;
                        physicalLocalRepository.Copy (e.Item);
                        break;
                    case EventType.DELETE:
                        SyncStatus = SyncStatus.DOWNLOADING;
                        physicalLocalRepository.Delete (e.Item);
                        break;
                    }                
                }
                
                logicalLocalRepository.Solve (e.Item);

                VerifySucess(e);

                eventDAO.UpdateToSynchronized(e);

                if(e.RepositoryType == RepositoryType.LOCAL){
                    new JSONHelper().postJSON (e);
                }

                Logger.LogEvent("DONE Event Synchronizing", e);
            } catch (AbortedOperationException abort){
                Logger.LogInfo("WARNING", "Operation abortet! \nCause: "+abort.Message);
                //TODO put  error flag on event
                eventDAO.UpdateToSynchronized(e);

            } catch (Exception exc){
                //TODO refactor to catch error and treat
                Logger.LogInfo("ERROR", exc);
            } finally {
                Thread.Sleep (1000);
            }
        }

        void VerifySucess (Event e)
        {
            SyncStatus = SyncStatus.VERIFING;

            //VerifyLocalChanges;
            //VerifyRemoteChanges
            //TODO atualizar item

            //REMOVE THIS ABOVE
            if (e.RepositoryType == RepositoryType.LOCAL){
                switch (e.EventType){
                    case EventType.MOVE:
                        UpdateeTag (e);
                        break;
                    case EventType.CREATE:
                        UpdateeTag (e);
                        break;
                    case EventType.UPDATE:
                        UpdateeTag (e);
                        break;
                    case EventType.COPY:
                        UpdateeTag (e);
                        break;
                    case EventType.DELETE:
                    break;
                }
            }else{
                switch (e.EventType){
                    case EventType.MOVE:
                        UpdateeTag (e);
                        break;
                    case EventType.CREATE:
                        UpdateeTag (e);
                        break;
                    case EventType.UPDATE:
                        UpdateeTag (e);
                        break;
                    case EventType.COPY:
                        UpdateeTag (e);
                        break;
                    case EventType.DELETE:
                    break;
                }                
            }
        }


        //TODO VERIFY!
        void UpdateeTag (Event e)
        {
            /*Logger.LogInfo("VERIFYING", "ETag verification...");
            if (e.Item.ResultObjectRelativePath.Length > 0) {
                e.Item.RemoteETAG = remoteRepository.RemoteETAG (e.Item.ResultObjectKey);
                e.Item.LocalETAG = new Crypto ().md5hash (e.Item.FullLocalResultObject);
            } else {
                e.Item.RemoteETAG = remoteRepository.RemoteETAG (e.Item.Key);
                e.Item.LocalETAG = new Crypto ().md5hash (e.Item.AbsolutePath);
            }

            if (!e.Item.RemoteETAG.Replace("\"", "").Equals (e.Item.LocalETAG))
                throw new QloudSync.VerificationException ();

            repositoryItemDAO.Update (e.Item);*/
        }
    }
}