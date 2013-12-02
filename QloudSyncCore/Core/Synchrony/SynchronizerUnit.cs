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
using System.Net.Sockets;
using LitS3;
using GreenQloud.Core;
using GreenQloud.Persistence.SQLite;

namespace GreenQloud.Synchrony
{
    
    public class SynchronizerUnit
    {
        private LocalRepository repo;
        private LocalEventsSynchronizer localSynchronizer;
        private RemoteEventsSynchronizer remoteSynchronizer;
        private RecoverySynchronizer recoverySynchronizer;
        private SynchronizerResolver synchronizerResolver;

        private static Dictionary<LocalRepository, SynchronizerUnit> synchronizerUnits = new Dictionary<LocalRepository, SynchronizerUnit>(new GreenQloud.Model.LocalRepository.LocalRepositoryComparer());

		public LocalEventsSynchronizer LocalEventsSynchronizer {
			get {
				return localSynchronizer;
			}
        }
        public RemoteEventsSynchronizer RemoteEventsSynchronizer
        {
            get
            {
                return remoteSynchronizer;
            }
        }
        public RecoverySynchronizer RecoverySynchronizer
        {
            get
            {
                return recoverySynchronizer;
            }
        }

        public static SynchronizerUnit GetByRepo (LocalRepository repo)
        {
            if (repo == null)
                return null;
            SynchronizerUnit unit;
            if (synchronizerUnits.TryGetValue (repo, out unit)) {
                return unit;
            } else {
                return null;
            }
        }

        public static void Add (LocalRepository repo, SynchronizerUnit unit)
        {
            synchronizerUnits.Add(repo, unit);
        }

        public static bool AnyDownloading ()
        {
            foreach(SynchronizerUnit unit in synchronizerUnits.Values){
                if (unit.IsDownloading) {
                    return true;
                }
            }
            return false;
        }
        
        public static int GetTotalEventsToSync()
        {
            int eventsToSync = 0;
            foreach (SynchronizerUnit unit in synchronizerUnits.Values)
            {
                eventsToSync += unit.synchronizerResolver.EventsToSync;
            }
            return eventsToSync;
        }

        public static bool AnyUploading ()
        {
            foreach(SynchronizerUnit unit in synchronizerUnits.Values){
                if (unit.IsUploading) {
                    return true;
                }
            }
            return false;
        }

        public static bool AnyWorking ()
        {
            foreach(SynchronizerUnit unit in synchronizerUnits.Values){
                if (unit.IsWorking) {
                    return true;
                }
            }
            return false;
        }
        
        public static void ReconnectAll ()
        {
            foreach(SynchronizerUnit unit in synchronizerUnits.Values){
                unit.Reconnect ();
            }
        }

        public static void DisconnectAll ()
        {
            foreach(SynchronizerUnit unit in synchronizerUnits.Values){
                unit.Disconnect ();
            }
        }

        public static void ReconnectResolver()
        {
            foreach (SynchronizerUnit unit in synchronizerUnits.Values) {
                unit.ResumeResolver ();
            }
        }

        public static void DisconnectResolver ()
        {
            foreach (SynchronizerUnit unit in synchronizerUnits.Values) {
                unit.SuspendResolver ();
            }
        }
        
        public SynchronizerUnit (LocalRepository repo)
        {
            this.repo = repo;
            synchronizerResolver = SynchronizerResolver.NewInstance (this.repo, this);
            recoverySynchronizer = RecoverySynchronizer.NewInstance (this.repo, this);
            remoteSynchronizer = RemoteEventsSynchronizer.NewInstance (this.repo, this);
            localSynchronizer = LocalEventsSynchronizer.NewInstance (this.repo, this);
        }

        public void InitializeSynchronizers (bool recovery = false)
        {
            if (recovery) {
                SQLiteEventDAO eventDao = new SQLiteEventDAO(this.repo);
                eventDao.RemoveAllUnsynchronized();
            }
            recoverySynchronizer.Start (); 
            localSynchronizer.Start ();
            remoteSynchronizer.Start ();
            synchronizerResolver.Start();
        }

        public void StopAll ()
        {
            if(synchronizerResolver != null)
                synchronizerResolver.Stop();
            if(recoverySynchronizer != null)
                recoverySynchronizer.Stop();
            if(localSynchronizer != null)
                localSynchronizer.Stop();
            if(remoteSynchronizer != null)
                remoteSynchronizer.Stop();
        }

        public void KillAll()
        {
            remoteSynchronizer.Kill();
            localSynchronizer.Kill();
            recoverySynchronizer.Kill();
            synchronizerResolver.Kill();
            
            if (remoteSynchronizer != null)
            {
                while (!remoteSynchronizer.Killed)
                    Thread.Sleep(1000);

                Logger.LogInfo("STOP THREAD", "Remote Synchronizer killed nicelly");
            }
            if (localSynchronizer != null)
            {
                while (!localSynchronizer.Killed)
                    Thread.Sleep(1000);

                Logger.LogInfo("STOP THREAD", "Local Synchronizer killed nicelly");
            }
            if (recoverySynchronizer != null)
            {
                while (!recoverySynchronizer.Killed)
                    Thread.Sleep(1000);

                Logger.LogInfo("STOP THREAD", "Recovery Synchronizer killed nicelly");
            }
            if (synchronizerResolver != null)
            {
                while (!synchronizerResolver.Killed)
                    Thread.Sleep(1000);

                Logger.LogInfo("STOP THREAD","Synchronizer Resolver killed nicelly");
            }
            synchronizerUnits.Remove(this.repo);
        }

        public void ResumeResolver()
        {
            if (synchronizerResolver != null) {
                synchronizerResolver.Start ();
            }
        }

        public void Reconnect ()
        {
			if(remoteSynchronizer != null)
				remoteSynchronizer.Start ();
			if(synchronizerResolver != null)
				synchronizerResolver.Start ();
            if (recoverySynchronizer != null)
                recoverySynchronizer.Start();
        }

        public void SuspendResolver(){
            if (synchronizerResolver != null) {
                synchronizerResolver.Stop ();
            }
        }

        public void Disconnect ()
        {
			if(remoteSynchronizer != null)
				remoteSynchronizer.Stop ();
			if(synchronizerResolver != null)
				synchronizerResolver.Stop ();
            if (recoverySynchronizer != null)
                recoverySynchronizer.Stop();
        }

        public bool IsWorking {
            get {
                if (synchronizerResolver.SyncStatus == SyncStatus.DOWNLOADING || synchronizerResolver.SyncStatus == SyncStatus.UPLOADING) {
                    return true;
                } else {
                    return false;
                }
			}
        }
        public bool IsDownloading {
			get {
                if (synchronizerResolver.SyncStatus == SyncStatus.DOWNLOADING) {
					return true;
				} else {
					return false;
				}
			}
        }
        public bool IsUploading {
            get {
                if (synchronizerResolver.SyncStatus == SyncStatus.UPLOADING) {
                    return true;
                } else {
                    return false;
                }
            }
        }
    }
}