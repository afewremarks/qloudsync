using System;
using GreenQloud.Model;
using GreenQloud.Repository.Local;
using GreenQloud.Repository.Remote;
using GreenQloud.Persistence;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Globalization;

namespace GreenQloud.Synchrony
{
    public class RemoteEventsSynchronizer : AbstractSynchronizer
    {
        Thread threadSync;
        bool eventsCreated;

        string LastSyncTime;

        public RemoteEventsSynchronizer  
            (LogicalRepositoryController logicalLocalRepository, PhysicalRepositoryController physicalLocalRepository, RemoteRepositoryController remoteRepository, TransferDAO transferDAO, EventDAO eventDAO) :
            base (logicalLocalRepository, physicalLocalRepository, remoteRepository, transferDAO, eventDAO)
        {
            threadSync = new Thread(() =>{
                Synchronize ();
            });
            LastSyncTime = string.Empty;
        }



        public new void Synchronize(){
            while (Working){
                if (eventsCreated){
                    eventsCreated = false;
                    if(SyncStatus == SyncStatus.IDLE){
                        base.Synchronize ();
                    }
                }
                Thread.Sleep (10000);
            }
        }

        bool ready=true;

        public void AddEvents ()
        {
            if (!ready)
                return;
            if (SyncStatus == SyncStatus.DOWNLOADING || SyncStatus == SyncStatus.UPLOADING)
                return;
            ready = false;
            string hash = Crypto.GetHMACbase64(Credential.SecretKey,Credential.PublicKey, true);
            string uri = string.Format ("https://my.greenqloud.com/qloudsync/history/{0}/?username={1}&hash={2}&createdDate={3}", RuntimeSettings.DefaultBucketName, Credential.Username, hash, LastSyncTime);
            Logger.LogInfo("StorageQloud", "Looking for new changes");
            foreach(Newtonsoft.Json.Linq.JObject jsonObject in JSONHelper.GetInfoArray(uri)){
                Event e = new Event();
                e.RepositoryType = RepositoryType.REMOTE;
                e.EventType = (EventType) Enum.Parse(typeof(EventType), (string)jsonObject["action"]);
                e.User = (string)jsonObject["username"];
                e.Application = (string)jsonObject["application"];
                e.ApplicationVersion = (string)jsonObject["applicationVersion"];
                e.DeviceId = (string)jsonObject["deviceId"];
                e.OS = (string)jsonObject["os"];
                e.Bucket = (string)jsonObject["bucket"];
                e.InsertTime = (string)jsonObject["createdDate"];
                string relativePath = (string)jsonObject["object"];
                e.Item = RepositoryItem.CreateInstance (new LocalRepository(RuntimeSettings.HomePath), relativePath, false, 0, e.InsertTime);
                e.Synchronized = false;
                eventDAO.Create(e);
                LastSyncTime = e.InsertTime;
                
                UpdateLastSyncTime();
            }
            eventsCreated = true;

            ready = true;
        }

        void UpdateLastSyncTime ()
        {
            int d = int.Parse (LastSyncTime.Substring(LastSyncTime.Length-2,2).ToString());

            if (d!=59){
                d++;
            }
            LastSyncTime = LastSyncTime.Substring(0,LastSyncTime.Length-2) + d.ToString();
           
        }

        public double InitFirstLoad ()
        {
            double size = 0;
            Start();
           
            foreach (RepositoryItem i in remoteRepository.Items){
                size+= i.Size;

                eventDAO.Create ( new Event(){
                    EventType = EventType.CREATE,
                    RepositoryType = RepositoryType.REMOTE,
                    Item = i,
                    Synchronized = false
                });
            }
            eventsCreated = true;
            return size;
        }


        #region implemented abstract members of Synchronizer
        public override void Start ()
        {
            Working = true;
            try{
                threadSync.Start();
            }catch{
                // do nothing
            }
        }
        public override void Pause ()
        {
            Working = false;
        }
        
        public override void Stop ()
        {
            Working = false;
            threadSync.Join();
        }
        #endregion  

        public void GenericSynchronize(){
            base.Synchronize();
        }
    }
}

