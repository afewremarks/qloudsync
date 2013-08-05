using System;
using GreenQloud.Model;
using GreenQloud.Repository.Local;
using GreenQloud.Persistence;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Globalization;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;
using GreenQloud.Repository;
using GreenQloud.Persistence.SQLite;
using System.IO;

namespace GreenQloud.Synchrony
{
    public class RemoteEventsSynchronizer : AbstractSynchronizer<RemoteEventsSynchronizer>
    {
        private bool eventsCreated;
        private IRemoteRepositoryController remoteController = new RemoteRepositoryController ();
        private SQLiteRepositoryDAO repositoryDAO = new SQLiteRepositoryDAO();
        private SQLiteEventDAO eventDAO = new SQLiteEventDAO();
        private RepositoryItemDAO repositoryItemDAO = new SQLiteRepositoryItemDAO();
        private IRemoteRepositoryController remoteRepository = new RemoteRepositoryController ();


        public RemoteEventsSynchronizer () : base ()
        {
        }

        public override void Run(){
            while (!_stoped){
                AddEvents();
                Thread.Sleep (5000);
            }
        }

        public void AddEvents ()
        {
            lock (lockk) {
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

                        string key = (string)jsonObject["object"];
                        bool isFolder;
                        if((string)jsonObject["resultObject"] == string.Empty){
                            isFolder = remoteController.GetMetadata(key).ContentLength==0;
                        } else {
                            isFolder = remoteController.GetMetadata((string)jsonObject["resultObject"]).ContentLength==0;
                        }

                        e.Item = RepositoryItem.CreateInstance (repositoryDAO.FindOrCreateByRootName(RuntimeSettings.HomePath), isFolder, key);
                        e.Item.BuildResultItem((string)jsonObject["resultObject"]);
                        e.Item.ETag = (string)jsonObject["hash"];

                        e.Synchronized = false;
                        eventDAO.Create(e);
                        repositoryItemDAO.Update(e.Item);
                    }
                }
                eventsCreated = true;
            }
        }

        public bool HasInit {
            get {
                return eventsCreated;
            }
        }
    }
}

