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
using LitS3;
using System.Linq;
using GreenQloud.Core;

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
                eventsToSync =  eventDAO.EventsNotSynchronized.Count;
                while (eventsToSync > 0 && !_stoped) {
                    Synchronize ();
                    eventsToSync = eventDAO.EventsNotSynchronized.Count;
                }
                SyncStatus = SyncStatus.IDLE;
                Done = true;
            }
            Thread.Sleep (1000);
        }

        private bool VerifyIgnoreRemote (Event remoteEvent)
        {
            GetObjectResponse meta = null;

            //Ignore events without metadata....
            if(remoteEvent.EventType != EventType.DELETE) {
                if (remoteEvent.HaveResultItem) { 
                    meta = remoteRepository.GetMetadata (remoteEvent.Item.ResultItem.Key);
                } else {
                    meta = remoteRepository.GetMetadata (remoteEvent.Item.Key);
                }
                if(meta == null){
                    Logger.LogInfo("ERROR", "File " + (remoteEvent.HaveResultItem ? remoteEvent.Item.ResultItem.Key : remoteEvent.Item.Key) + " ignored. Metadata not found!");
                    return true;
                }
            }


            if(!remoteEvent.HaveResultItem){
                if (!remoteEvent.Item.IsFolder) {
                    if (remoteRepository.Exists(remoteEvent.Item) && meta.ContentLength == 0)
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

        void Synchronize(){
            Exception currentException;
            Event e = eventDAO.EventsNotSynchronized.FirstOrDefault();
            do {
                currentException = null;
                if (e  != null){
                    try {
                        PerformIgnores (e);
                        if (VerifyIgnore (e)) {
                            eventDAO.UpdateToSynchronized (e, RESPONSE.IGNORED);
                            Logger.LogInfo ("EVENT IGNORE", "Ignore event on " + e.Item.LocalAbsolutePath);
                            return;
                        }

                        //refresh event
                        e = eventDAO.FindById(e.Id);
                        if(e.Synchronized){
                            Logger.LogInfo ("INFO", "Event " + e.Id + " already synchronized with response " + e.Response);
                            return;
                        }
                        Logger.LogEvent ("Event Synchronizing (try "+(e.TryQnt+1)+")", e );
                        if (e.RepositoryType == RepositoryType.LOCAL) {
                            SyncStatus = SyncStatus.UPLOADING;
                            Program.Controller.HandleSyncStatusChanged ();

                            switch (e.EventType) {
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
                        } else {
                            SyncStatus = SyncStatus.DOWNLOADING;
                            Program.Controller.HandleSyncStatusChanged ();

                            switch (e.EventType) {
                            case EventType.MOVE:
                                physicalLocalRepository.Move (e.Item);
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
                        
                        VerifySucess (e);

                        if (e.RepositoryType == RepositoryType.LOCAL) {
                            new JSONHelper ().postJSON (e);
                        }
                        eventDAO.UpdateToSynchronized (e, RESPONSE.OK);

                        SyncStatus = SyncStatus.IDLE;
                        Program.Controller.HandleSyncStatusChanged ();

                        Logger.LogEvent ("DONE Event Synchronizing", e);
                    } catch (WebException webx) {
                        if (webx.Status == WebExceptionStatus.NameResolutionFailure || webx.Status == WebExceptionStatus.Timeout || webx.Status == WebExceptionStatus.ConnectFailure) {
                            throw webx;
                        } else {
                            currentException = webx;
                        }
                    } catch (SocketException sock) {
                        throw sock;
                    } catch (Exception ex) {
                        currentException = ex;
                    } 

                    e.TryQnt++;
                    eventDAO.UpdateTryQnt (e);
                }

                if(currentException != null){
                    Thread.Sleep(5000);
                }

            } while (currentException != null && e.TryQnt < 5 && !_stoped);

            if (currentException != null) {
                throw currentException;
            }
        }

        void PerformIgnores (Event e)
        {
            eventDAO.IgnoreAllEquals(e);
            eventDAO.IgnoreAllIfDeleted(e);
            eventDAO.IgnoreAllIfMoved(e);
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