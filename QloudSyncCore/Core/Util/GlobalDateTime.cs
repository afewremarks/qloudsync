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
        private static double diff;
        private static bool loadedDiff = false;
        public static DateTime Now {
            get{
                return NowFromDiff;
            }
        }
        public static DateTime NowFromDiff {
            get {
                if (!loadedDiff) {
                    diff = new SQLiteTimeDiffDAO ().Last;
                    loadedDiff = true;
                }
                DateTime local = DateTime.Now;
                DateTime diffTime = local.AddMilliseconds (diff);
                return diffTime;
            }
        }

        public static string ToHumanReadableString(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays > 30) {
                double month = Math.Floor ((timeSpan.TotalDays / 30));
                if (month == 1)
                    return  month + " month ago";
                else
                    return  month + "months ago";
            } else if (timeSpan.TotalDays > 7) {
                double week = Math.Floor ((timeSpan.TotalDays / 7));
                if (week == 1)
                    return   week + " week ago";
                else
                    return   week + " weeks ago";
            } else if (timeSpan.TotalDays > 1) {
                double day = Math.Floor (timeSpan.TotalDays);
                if(day == 1)
                    return day + " day ago";
                else
                    return day + " days ago";
            } else {
                if (timeSpan.TotalHours >= 1) {
                    double hour = Math.Floor (timeSpan.TotalHours);
                    if (hour == 1)
                        return hour + " hour ago";
                    else
                        return hour + " hours ago";
                } else if (timeSpan.TotalMinutes >= 1) {
                    return Math.Floor (timeSpan.TotalMinutes) + " minutes ago";
                } else {
                    return "Just now";
                }
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

