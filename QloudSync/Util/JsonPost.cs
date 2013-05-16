using Amazon.S3.Model;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Threading;
using Newtonsoft;


using GreenQloud.Model;
using GreenQloud.Repository;
using GreenQloud.Util;
using GreenQloud.Persistence;
using GreenQloud.Repository.Local;
using GreenQloud.Repository.Remote;

namespace GreenQloud

{
    public class JSON_POST
    {
        public void send (Event e)
        {
            postJSON(e);
        }

        public string postJSON (Event e)
        {

            string hash = Crypto.GetHMACbase64(Credential.SecretKey,Credential.PublicKey, true);
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(string.Format("https://my.greenqloud.com/qloudsync/history/{0}?username={1}&hash={2}",RuntimeSettings.DefaultBucketName, Credential.Username, hash));
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";
            string json = "";
            string result = null;
            
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {

 
                json = "{\"action\":\""+e.EventType+"\"," +
                    "\"application\":\""+GlobalSettings.ApplicationName+"\"," + "\"applicationVersion\":\""+GlobalSettings.RunningVersion+"\"," + "\"bucket\":\""+Credential.Username+""+GlobalSettings.SuffixNameBucket+"\"," + "\"deviceId\":\"unknown\"," + "\"hash\":\""+hash+"\"," + "\"object\":\"" + e.Item.FullLocalName +"\"," + "\"os\":\"unknown\"," + "\"resultObject\":\""+e.ResultObject+"\"," + "\"username\":\""+Credential.Username+"\"}";
            
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();

            }
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    
                      result = streamReader.ReadToEnd();

                    
                }
            return result;
            
        }
    }

}