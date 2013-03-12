using System;
using System.Net;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;

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
            JObject data = GetInfo (uri);
            co2savings.Used = (string)data["used"];
            co2savings.Saved = (string)data["saved"];
            return co2savings;
        }

        private static string GetVersionAvaliable(){

            return (string)GetInfo ("https://my.greenqloud.com/qloudsync/version")["version"];
        }

        private static JObject GetInfo (string url)
        {
            System.Net.WebRequest myReq = System.Net.WebRequest.Create (url);
            string receiveContent = string.Empty;
            try {
                using (System.Net.WebResponse wr = myReq.GetResponse ()) {
                    Stream receiveStream = wr.GetResponseStream ();
                    StreamReader reader = new StreamReader (receiveStream, System.Text.Encoding.UTF8);
                    receiveContent = reader.ReadToEnd ();
                
                }
                
                return Newtonsoft.Json.Linq.JObject.Parse(receiveContent);
            } catch {
                return new JObject();
            }
        }
    }
}

