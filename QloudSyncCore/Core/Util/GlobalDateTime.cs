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
	public class GlobalDateTime
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
                DateTime diffTime = local.AddMilliseconds (diff);
                return diffTime;
            }
        }

        public static string ToHumanReadableString(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays > 30)
                return Math.Floor((timeSpan.TotalDays / 30)) + " month(s) ago";
            if (timeSpan.TotalDays > 7)
                return  Math.Floor((timeSpan.TotalDays / 7)) + " week(s) ago";
            if (timeSpan.TotalDays < 1) {
                if (timeSpan.TotalHours >= 1) {
                    return Math.Floor (timeSpan.TotalHours) + " hours(s) ago";
                } else if (timeSpan.TotalMinutes >= 1) {
                    return Math.Floor (timeSpan.TotalMinutes) + " mins(s) ago";
                } else {
                    return "Just now";
                }
            }
            return Math.Floor(timeSpan.TotalDays) + " day(s) ago";
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

