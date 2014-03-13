using System;
using GreenQloud.Model;
using GreenQloud.Repository;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Globalization;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;
using System.IO;
using GreenQloud.Persistence.SQLite;
using System.Net;
using System.Net.Sockets;

namespace GreenQloud.Synchrony
{
    public class RemoteEventsSynchronizer : AbstractSynchronizer<RemoteEventsSynchronizer>
    {
        private IRemoteRepositoryController remoteController;
        private SQLiteEventDAO eventDAO;
        private SQLiteRepositoryItemDAO repositoryItemDAO;
        private IRemoteRepositoryController remoteRepository;


        public RemoteEventsSynchronizer (LocalRepository repo, SynchronizerUnit unit) : base (repo, unit)
        {
            remoteController = new RemoteRepositoryController (repo);
            eventDAO = new SQLiteEventDAO(repo);
            repositoryItemDAO = new SQLiteRepositoryItemDAO();
            remoteRepository = new RemoteRepositoryController (repo);
        }

        public override void Run(){
            while (!Stoped)
           {
                AddEvents();
                Wait(30000);
           }
        }

        public void AddEvents ()
        {
            Exception currentException;
            int tryQnt = 0;
            do {
                try{
                    tryQnt ++;
                    currentException = null;
                    string hash = Crypto.GetHMACbase64(Credential.SecretKey,Credential.PublicKey, false);
                    DateTime time = eventDAO.LastSyncTime;
                    time = time.AddSeconds(1);
                    Logger.LogInfo("INFO HISTORY REQUEST", "Looking for new changes on " + repo.RemoteFolder + " ["+time+"]");

                    UrlEncode encoder = new UrlEncode();
                    string uri = string.Format("https://my.greenqloud.com/qloudsync/history/{0}/?username={1}&hashValue={2}&createdDate={3}", encoder.Encode(RuntimeSettings.DefaultBucketName), encoder.Encode(Credential.Username), encoder.Encode(hash), encoder.Encode(time.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'")));
                    Logger.LogInfo("INFO HISTORY REQUEST", uri);

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
                                e.InsertTime = ((DateTime)jsonObject["createdDate"]);
                                e.Item = RepositoryItem.CreateInstance (repo, key);
                                e.Item.BuildResultItem((string)jsonObject["resultObject"]);
                                e.Item.ETag = (string)jsonObject["hash"];
                                e.Synchronized = false;
                                eventDAO.Create(e);
                                repositoryItemDAO.Update(e.Item);
                                Logger.LogEvent("EVENT FOUND ON HISTORY", e);
                            }
                        }
                    }
                    canChange = true;
                } catch (WebException webx) {
                    Logger.LogInfo("ERROR COMMUNICATION FAILURE ON HISTORY", webx);
                    currentException = webx;
                } catch (SocketException sock) {
                    Logger.LogInfo("ERROR COMMUNICATION FAILURE ON HISTORY", sock);
                    currentException = sock;
                } catch (Exception ex) {
                    Logger.LogInfo("ERROR COMMUNICATION FAILURE ON HISTORY", ex);
                    currentException = ex;
                }
                if(currentException != null){
                    Wait(10000);
                }
            } while (currentException != null && tryQnt < 5 && !Stoped);
            if (currentException != null) {
                throw currentException;
            }
        }

    }
}

