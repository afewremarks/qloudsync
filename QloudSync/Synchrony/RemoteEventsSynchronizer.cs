using System;
using GreenQloud.Model;
using GreenQloud.Repository.Local;
using GreenQloud.Repository.Remote;
using GreenQloud.Persistence;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Globalization;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;

namespace GreenQloud.Synchrony
{
    public class RemoteEventsSynchronizer : AbstractSynchronizer
    {
        Thread threadSync;
        bool eventsCreated;

        public RemoteEventsSynchronizer  
            (LogicalRepositoryController logicalLocalRepository, PhysicalRepositoryController physicalLocalRepository, RemoteRepositoryController remoteRepository, TransferDAO transferDAO, EventDAO eventDAO, RepositoryItemDAO repositoryItemDAO) :
                base (logicalLocalRepository, physicalLocalRepository, remoteRepository, transferDAO, eventDAO, repositoryItemDAO)
        {
            threadSync = new Thread(() =>{
                Synchronize ();
            });
        }



        public new void Synchronize(){
            while (Working){
                Thread.Sleep (20000);
                if (eventsCreated){
                    eventsCreated = false;
                    //if(SyncStatus == SyncStatus.IDLE){
                        //base.Synchronize ();
                    //}
                }
            }
        }

        bool ready=true;
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void AddEvents ()
        {
            try{
                if (!ready)
                    return;
                //if (SyncStatus == SyncStatus.DOWNLOADING || SyncStatus == SyncStatus.UPLOADING)
                //    return;
                ready = false;
                string hash = Crypto.GetHMACbase64(Credential.SecretKey,Credential.PublicKey, false);
                string time = eventDAO.LastSyncTime;
                Logger.LogInfo("StorageQloud", "Looking for new changes ["+time+"]");

                UrlEncode encoder = new UrlEncode();
                string uri = string.Format ("https://my.greenqloud.com/qloudsync/history/{0}/?username={1}&hash={2}&createdDate={3}", encoder.Encode (RuntimeSettings.DefaultBucketName), encoder.Encode (Credential.Username), encoder.Encode (hash), encoder.Encode (time));

                JArray jsonObjects = JSONHelper.GetInfoArray(uri);
                foreach(Newtonsoft.Json.Linq.JObject jsonObject in jsonObjects){
                    if(!((string)jsonObject["application"]).Equals(GlobalSettings.FullApplicationName)){
                        Event e = new Event();
                        e.RepositoryType = RepositoryType.REMOTE;
                        e.EventType = (EventType) Enum.Parse(typeof(EventType), (string)jsonObject["action"]);
                        e.User = (string)jsonObject["username"];
                        e.Application = (string)jsonObject["application"];
                        e.ApplicationVersion = (string)jsonObject["applicationVersion"];
                        e.DeviceId = (string)jsonObject["deviceId"];
                        e.OS = (string)jsonObject["os"];
                        e.Bucket = (string)jsonObject["bucket"];

                        e.InsertTime = ((DateTime)jsonObject["createdDate"]).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");

                        string relativePath = (string)jsonObject["object"];
                        e.Item = RepositoryItem.CreateInstance (new LocalRepository(RuntimeSettings.HomePath), relativePath, false, 0, e.InsertTime);

                        e.Item.ResultObjectRelativePath = (string)jsonObject["resultObject"];
                        e.Item.RemoteETAG = (string)jsonObject["hash"];

                        e.Synchronized = false;
                        eventDAO.Create(e);
                        repositoryItemDAO.Update(e.Item);
                    }
                }
                eventsCreated = true;
            } catch (Exception e){
                Logger.LogInfo("ERROR", e);
            }
            ready = true;
        }

        public bool HasInit {
            get {
                return eventsCreated;
            }
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
            //base.Synchronize();
        }
    }
}

