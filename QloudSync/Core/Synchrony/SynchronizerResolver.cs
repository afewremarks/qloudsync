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

        protected EventDAO eventDAO = new SQLiteEventDAO();
        protected RepositoryItemDAO repositoryItemDAO = new SQLiteRepositoryItemDAO ();
        protected IPhysicalRepositoryController physicalLocalRepository = new StorageQloudPhysicalRepositoryController ();
        protected RemoteRepositoryController remoteRepository = new RemoteRepositoryController();


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
        
        private SynchronizerResolver () : base ()
        {
            SyncStatus = SyncStatus.IDLE;
            threadSync = new Thread(() =>{
                Synchronize ();
            });
        }

        public static SynchronizerResolver GetInstance(){
            if (instance == null)
                instance = new SynchronizerResolver ();
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
                SolveAll ();
            }
        }

        public void SolveAll ()
        {
            List<Event> eventsNotSynchronized = eventDAO.EventsNotSynchronized;
            eventsToSync = eventsNotSynchronized.Count;
            while (eventsNotSynchronized.Count > 0) {
                Synchronize (eventsNotSynchronized [0]);
                eventsNotSynchronized = eventDAO.EventsNotSynchronized;
            }
            SyncStatus = SyncStatus.IDLE;
            Done = true;
            Thread.Sleep (1000);
        }

        //TODO refactor ignores
        private bool VerifyIgnoreRemote (Event remoteEvent)
        {
            if(remoteEvent.Item.ResultItem == null){
                if (!remoteEvent.Item.IsFolder) {
                    if (remoteRepository.Exists(remoteEvent.Item) && remoteRepository.GetMetadata(remoteEvent.Item.Key).ContentLength == 0)
                        return true;
                }
            }
            return false;
        }
        private bool VerifyIgnoreLocal (Event localEvent)
        {
            if(localEvent.Item.ResultItem == null){
                if (!localEvent.Item.IsFolder) {
                    FileInfo fi = new FileInfo (localEvent.Item.LocalAbsolutePath);
                    if (fi.Exists && fi.Length == 0)
                        return true;
                }
            }
            return false;
        }

        private bool VerifyIgnore (Event e)
        {
            if (e.Item.Key.StartsWith ("."))
                return true;
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
                
                VerifySucess(e);

                eventDAO.UpdateToSynchronized(e);

                if(e.RepositoryType == RepositoryType.LOCAL){
                    new JSONHelper().postJSON (e);
                }

                Logger.LogEvent("DONE Event Synchronizing", e);
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

            if (e.RepositoryType == RepositoryType.LOCAL){
                switch (e.EventType){
                    case EventType.MOVE:
                        UpdateETag (e);
                        e.Item.Moved = true;
                        repositoryItemDAO.MarkAsMoved (e.Item);
                        break;
                    case EventType.CREATE:
                        UpdateETag (e);
                        break;
                    case EventType.UPDATE:
                        UpdateETag (e);
                        break;
                    case EventType.COPY:
                        UpdateETag (e);
                        break;
                    case EventType.DELETE:
                        e.Item.Moved = true;
                        repositoryItemDAO.MarkAsMoved (e.Item);
                    break;
                }
            }else{
                switch (e.EventType){
                    case EventType.MOVE:
                        UpdateETag (e);
                        e.Item.Moved = true;
                        repositoryItemDAO.MarkAsMoved (e.Item);
                        break;
                    case EventType.CREATE:
                        UpdateETag (e);
                        break;
                    case EventType.UPDATE:
                        UpdateETag (e);
                        break;
                    case EventType.COPY:
                        UpdateETag (e);
                        break;
                    case EventType.DELETE:
                        e.Item.Moved = true;
                        repositoryItemDAO.MarkAsMoved (e.Item);
                    break;
                }                
            }
        }


        void UpdateETag (Event e)
        {
            if (e.Item.ResultItem != null) {
                e.Item.ResultItem.ETag = remoteRepository.RemoteETAG (e.Item.ResultItem);
                e.Item.ResultItem.LocalETag = new Crypto ().md5hash (e.Item.ResultItem);
                if (!e.Item.ResultItem.ETag.Equals (e.Item.ResultItem.LocalETag))
                    throw new QloudSync.VerificationException ();
            } else {
                e.Item.ETag = remoteRepository.RemoteETAG (e.Item);
                e.Item.LocalETag = new Crypto ().md5hash (e.Item);
                if (!e.Item.ETag.Equals (e.Item.LocalETag))
                    throw new QloudSync.VerificationException ();
            }

            repositoryItemDAO.Update (e.Item);
        }
    }
}