using System;
using Newtonsoft.Json.Linq;
using System.IO;

namespace GreenQloud
{
    public class JSONHelper
    {
        public static JObject GetInfo (string url)
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
            } catch (Exception e){
                Console.WriteLine(e.Data);
                return new JObject();
            }
        }

        public static JArray GetInfoArray (string url)
        {
            System.Net.WebRequest myReq = System.Net.WebRequest.Create (url);
            string receiveContent = string.Empty;
            try {
                using (System.Net.WebResponse wr = myReq.GetResponse ()) {
                    Stream receiveStream = wr.GetResponseStream ();
                    StreamReader reader = new StreamReader (receiveStream, System.Text.Encoding.UTF8);
                    receiveContent = reader.ReadToEnd ();
                    
                }
                
                return Newtonsoft.Json.Linq.JArray.Parse(receiveContent);
            } catch (Exception e){
                Console.WriteLine(e.Data);
                throw new DisconnectionException();
            }
        }
    }
}

