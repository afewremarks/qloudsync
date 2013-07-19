using System;
using System.Net;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
using GreenQloud.Model;

namespace GreenQloud
{
    public class Statistics
    {

        private static System.Timers.Timer statistic_timer         = new System.Timers.Timer () { Interval = GlobalSettings.IntervalBetweenChecksStatistics };

        private Statistics(){
        }

        public static void Init(){
            statistic_timer.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) => {UpdateVariables();};
            statistic_timer.Disposed += (object sender, EventArgs e) => Console.WriteLine("Dispose Statistics");
        }

        public static void Stop(){
            statistic_timer.Stop();
            statistic_timer.Dispose ();
        }

        static void UpdateVariables(){
            versionAvailable = GetVersionAvaliable ();
            savings = GetCO2Statistics ();
            usedup = GetSQTotalUsed ();
        }

        static string versionAvailable = string.Empty;
        public static string VersionAvailable {
            get {
                if(versionAvailable==string.Empty)
                    versionAvailable = GetVersionAvaliable();              

                return versionAvailable;
            }
        }

        static CO2Savings savings = null;
        public static CO2Savings EarlyCO2Savings {
            get {
                if(savings==null){
                    savings = GetCO2Statistics();
                }

                return savings;
            }
        }

        private static CO2Savings GetCO2Statistics (){

            CO2Savings co2savings = new CO2Savings();
            string hash = Crypto.GetHMACbase64(Credential.SecretKey,Credential.PublicKey, true);

            string uri = string.Format("https://my.greenqloud.com/qloudsync/metrics/?username={0}&hash={1}", Credential.Username, hash);
            JObject data = JSONHelper.GetInfo (uri);
            if(data != null && data ["trulygreen"] != null){
                foreach(JToken o in data ["trulygreen"]){
                    if((string)o["id"] == "co2_savings_total")
                        co2savings.Saved = (string) o["value"] ;
                }
            }
            return co2savings;
        }

        static SQTotalUsed usedup = null;
        public static SQTotalUsed TotalUsedSpace {
            get {
                if(usedup==null){
                    usedup = GetSQTotalUsed();
                }

                return usedup;
            }
        }

        private static SQTotalUsed GetSQTotalUsed (){

            SQTotalUsed sqtotalused = new SQTotalUsed();
            string hash = Crypto.GetHMACbase64(Credential.SecretKey,Credential.PublicKey, true);

            string uri = string.Format("https://my.greenqloud.com/qloudsync/metrics/?username={0}&hash={1}", Credential.Username, hash);
            JObject data = JSONHelper.GetInfo (uri);
            if(data != null && data ["usage"] != null){
                foreach(JToken o in data ["usage"]){
                    if((string)o["id"] == "storageqloud_size")
                        sqtotalused.Spent = (string) o["value"] ;
                }
            }
            return sqtotalused;
        }

        private static string GetVersionAvaliable(){

            return (string)JSONHelper.GetInfo ("https://my.greenqloud.com/qloudsync/version")["version"];
        }

  
    }
}

