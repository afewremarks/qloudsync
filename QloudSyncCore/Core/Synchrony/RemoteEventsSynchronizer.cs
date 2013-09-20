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
        private IRemoteRepositoryController remoteController;
        private SQLiteEventDAO eventDAO;
        private RepositoryItemDAO repositoryItemDAO;
        private IRemoteRepositoryController remoteRepository;


        public RemoteEventsSynchronizer (LocalRepository repo) : base (repo)
        {
            remoteController = new RemoteRepositoryController (repo);
            eventDAO = new SQLiteEventDAO(repo);
            repositoryItemDAO = new SQLiteRepositoryItemDAO();
            remoteRepository = new RemoteRepositoryController (repo);
        }

        public override void Run(){
            while (!_stoped){
                AddEvents();
                Thread.Sleep (5000);
            }
        }

        public void AddEvents ()
        {
            string hash = Crypto.GetHMACbase64(Credential.SecretKey,Credential.PublicKey, false);
            string time = eventDAO.LastSyncTime;
            Logger.LogInfo("StorageQloud", "Looking for new changes on " + repo.RemoteFolder + " ["+time+"]");

            UrlEncode encoder = new UrlEncode();
            string uri = string.Format ("https://my.greenqloud.com/qloudsync/history/{0}/?username={1}&hashValue={2}&createdDate={3}", encoder.Encode (RuntimeSettings.DefaultBucketName), encoder.Encode (Credential.Username), encoder.Encode (hash), encoder.Encode (time));

            JArray jsonObjects = JSONHelper.GetInfoArray(uri);
            foreach(Newtonsoft.Json.Linq.JObject jsonObject in jsonObjects){
                if(jsonObject["application"] != null && !((string)jsonObject["application"]).Equals(GlobalSettings.FullApplicationName)){
                    string key = (string)jsonObject["object"];
                    if(repo.Accepts(key)){
                        Event e = new Event(repo);
                        e.RepositoryType = RepositoryType.REMOTE;
                        e.EventType = (EventType) Enum.Parse(typeof(EventType), (string)jsonObject["action"]);
                        e.User = (string)jsonObject["username"];
                        e.Application = (string)jsonObject["application"];
                        e.ApplicationVersion = (string)jsonObject["applicationVersion"];
                        e.DeviceId = (string)jsonObject["deviceId"];
                        e.OS = (string)jsonObject["os"];
                        e.Bucket = (string)jsonObject["bucket"];
                        e.InsertTime = ((DateTime)jsonObject["createdDate"]).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
                        e.Item = RepositoryItem.CreateInstance (repo, key);
                        e.Item.BuildResultItem((string)jsonObject["resultObject"]);
                        e.Item.ETag = (string)jsonObject["hash"];
                        e.Synchronized = false;
                        eventDAO.Create(e);
                        repositoryItemDAO.Update(e.Item);
                    }
                }
            }
            eventsCreated = true;
        }

        public bool HasInit {
            get {
                return eventsCreated;
            }
        }
    }
}

