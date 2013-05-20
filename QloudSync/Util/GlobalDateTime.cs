using System;
using GreenQloud.Repository.Local;
using GreenQloud.Repository.Remote;
using GreenQloud.Persistence;
using GreenQloud.Model;
using System.Threading;
using GreenQloud.Persistence.SQLite;
using System.Collections.Generic;
using System.IO;

namespace GreenQloud
{
	class GlobalDateTime
	{
        public static DateTime Now {
            get{

                System.Net.WebRequest myReq = System.Net.WebRequest.Create ("https://my.greenqloud.com/qloudsync/servertime");
                string receiveContent = string.Empty;
                try {
                    using (System.Net.WebResponse wr = myReq.GetResponse ()) {
                        Stream receiveStream = wr.GetResponseStream ();
                        StreamReader reader = new StreamReader (receiveStream, System.Text.Encoding.UTF8);
                        receiveContent = reader.ReadToEnd ();

                    }

                    DateTime date = (DateTime) Newtonsoft.Json.Linq.JObject.Parse(receiveContent)["serverTime"];
                    return date;
                } catch (Exception e){
                    Logger.LogInfo("ERROR", "Cannot find global time on server, using local time.");
                    Logger.LogInfo("ERROR", e);
                    return GlobalDateTime.Now;
                }
            }
        }
	}
}

