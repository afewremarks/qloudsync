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
using System.Net.Sockets;

namespace GreenQloud.Synchrony
{
    
    public enum SyncStatus{
        IDLE,
        UPLOADING,
        DOWNLOADING,
        VERIFING
    }

    public class SynchronizerResolver : AbstractSynchronizer<SynchronizerResolver>
    {
        private SyncStatus status;
        protected EventDAO eventDAO = new SQLiteEventDAO();
        protected RepositoryItemDAO repositoryItemDAO = new SQLiteRepositoryItemDAO ();
        protected IPhysicalRepositoryController physicalLocalRepository = new StorageQloudPhysicalRepositoryController ();
        protected RemoteRepositoryController remoteRepository = new RemoteRepositoryController();


        public delegate void SyncStatusChangedHandler (SyncStatus status);
        public event SyncStatusChangedHandler SyncStatusChanged = delegate {};

        public SynchronizerResolver () : base ()
        {
        }

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

        //TODO CREATE a query for this
        public string[] Warnings {
            get {
                return new string[0];
            }
        }

        public bool Done {
            set; get;
        }

        public override void Run(){
            while (!_stoped){
                SolveAll ();
            }
        }

        public void SolveAll ()
        {
            lock (lockk) {
                List<Event> eventsNotSynchronized = eventDAO.EventsNotSynchronized.ToList();
                eventsToSync = eventsNotSynchronized.Count;
                while (eventsNotSynchronized.Count > 0) {
                    Event eventNotSynchronized = eventsNotSynchronized.First();
                    Synchronize (eventNotSynchronized);
                    eventsNotSynchronized = eventDAO.EventsNotSynchronized.ToList();
                    eventsToSync = eventsNotSynchronized.Count;
                }
                SyncStatus = SyncStatus.IDLE;
                Done = true;
            }
            Thread.Sleep (1000);
        }

        //TODO refactor ignores
        private bool VerifyIgnoreRemote (Event remoteEvent)
        {
            if(!remoteEvent.HaveResultItem){
                if (!remoteEvent.Item.IsFolder) {
                    if (remoteRepository.Exists(remoteEvent.Item) && remoteRepository.GetMetadata(remoteEvent.Item.Key).ContentLength == 0)
                        return true;
                }
            }
            return false;
        }
        private bool VerifyIgnoreLocal (Event localEvent)
        {
            if(!localEvent.HaveResultItem){
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
            if (e.Item.Name.StartsWith ("."))
                return true;
            if(e.RepositoryType == RepositoryType.REMOTE)
               return VerifyIgnoreRemote (e);
            if(e.RepositoryType == RepositoryType.LOCAL)
                return VerifyIgnoreLocal (e);

            return false;
        }

        void Synchronize(Event e){
            PerformIgnores (e);
            if (VerifyIgnore (e)) {
                eventDAO.UpdateToSynchronized(e);
                Logger.LogInfo ("EVENT IGNORE", "Ignore event on " + e.Item.LocalAbsolutePath);
                return;
            }
            Logger.LogEvent("Event Synchronizing", e);
            if (e.RepositoryType == RepositoryType.LOCAL){
                SyncStatus = SyncStatus.UPLOADING;
                Program.Controller.HandleSyncStatusChanged ();

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
                SyncStatus = SyncStatus.DOWNLOADING;
                Program.Controller.HandleSyncStatusChanged ();

                switch (e.EventType){
                case EventType.MOVE:
                    physicalLocalRepository.Move(e.Item);
                    break;
                case EventType.CREATE: 
                case EventType.UPDATE:
                    remoteRepository.Download (e.Item);
                    break;
                case EventType.COPY:
                    physicalLocalRepository.Copy (e.Item);
                    break;
                case EventType.DELETE:
                    physicalLocalRepository.Delete (e.Item);
                    break;
                }                
            }
            
            VerifySucess(e);

            if(e.RepositoryType == RepositoryType.LOCAL){
                new JSONHelper().postJSON (e);
            }
            eventDAO.UpdateToSynchronized(e);

            SyncStatus = SyncStatus.IDLE;
            Program.Controller.HandleSyncStatusChanged ();

            Logger.LogEvent("DONE Event Synchronizing", e);
        }

        void PerformIgnores (Event e)
        {
            eventDAO.IgnoreEquals(e);
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

            e.Item.UpdatedAt = GlobalDateTime.NowUniversalString;
            repositoryItemDAO.ActualizeUpdatedAt (e.Item);
            if(e.HaveResultItem){
                e.Item.ResultItem.UpdatedAt = GlobalDateTime.NowUniversalString;
                repositoryItemDAO.ActualizeUpdatedAt (e.Item.ResultItem);
            }
        }


        void UpdateETag (Event e)
        {
            if (e.HaveResultItem) {
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

            repositoryItemDAO.UpdateETAG (e.Item);
        }
    }
}