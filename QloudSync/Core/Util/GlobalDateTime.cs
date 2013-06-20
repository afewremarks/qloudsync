using System;
using GreenQloud.Repository.Local;
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
                return NowFromDiff;
            }
        }
        public static DateTime NowFromDiff {
            get {
                SQLiteTimeDiffDAO dao = new SQLiteTimeDiffDAO ();
                double diff = dao.Last;
                DateTime local = DateTime.Now;
                return local.AddMilliseconds(diff);
            }
        }

        public static DateTime NowRemote {
            get{
                DateTime date;
                System.Net.WebRequest myReq = System.Net.WebRequest.Create ("https://my.greenqloud.com/qloudsync/servertime");
                string receiveContent = string.Empty;

                using (System.Net.WebResponse wr = myReq.GetResponse ()) {
                    Stream receiveStream = wr.GetResponseStream ();
                    StreamReader reader = new StreamReader (receiveStream, System.Text.Encoding.UTF8);
                    receiveContent = reader.ReadToEnd ();

                }

                date = (DateTime) Newtonsoft.Json.Linq.JObject.Parse(receiveContent)["serverTime"];
                return date;
            }
        }

        public static string NowUniversalString {
            get{
                return Now.ToString ("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
            }
        }

        public static void CalcTimeDiff() {
            SQLiteTimeDiffDAO dao = new SQLiteTimeDiffDAO ();
            DateTime remoteDate = GlobalDateTime.NowRemote;
            DateTime localDate = DateTime.Now;
            double diff = (remoteDate - localDate).TotalMilliseconds;
            dao.Create (diff);
        }

	}
}

