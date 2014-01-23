using System;
using GreenQloud;
using System.Net;
using System.IO;
using System.Text;
using LitS3;
using System.Collections.Specialized;

namespace QloudSync.Repository
{
    public class S3Connection
    {
        public S3Connection ()
        {
        }

        public static void Authenticate (string username, string password)
        {
            Uri uri;

            WebRequest myReq;
            if (GlobalSettings.UseAsReseller)
            {
                NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
                queryString["username"] =  username;
                queryString["password"] = password;

                uri = new Uri(GlobalSettings.AuthenticationURL + "?" + queryString.ToString());
                myReq = WebRequest.Create(uri);
                using (WebResponse wr = myReq.GetResponse ()){
                    Stream receiveStream = wr.GetResponseStream ();
                    StreamReader reader = new StreamReader (receiveStream, Encoding.UTF8);
                    string receiveContent = reader.ReadToEnd ();
                    Newtonsoft.Json.Linq.JObject o = Newtonsoft.Json.Linq.JObject.Parse(receiveContent);               
                    Credential.SecretKey = (string)o["api_private_key"];
                    Credential.PublicKey = (string)o["api_public_key"];
                }
            }
            else 
            {

                uri = new Uri(GlobalSettings.AuthenticationURL);
                myReq = WebRequest.Create(uri);
                myReq.ContentType = "application/json";
                myReq.Method = "POST";
                string json = "";
                using (var streamWriter = new StreamWriter(myReq.GetRequestStream()))
                {
                    json = "{\"username\":\""+username+"\",\"password\":\""+password+"\"}";
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
                using (WebResponse wr = myReq.GetResponse ()){
                    Stream receiveStream = wr.GetResponseStream ();
                    StreamReader reader = new StreamReader (receiveStream, Encoding.UTF8);
                    string receiveContent = reader.ReadToEnd ();
                    Newtonsoft.Json.Linq.JObject o = Newtonsoft.Json.Linq.JObject.Parse(receiveContent);               
                    Credential.SecretKey = (string)o["secretKey"];
                    Credential.PublicKey = (string)o["apiKey"];
                }
            }

            Logger.LogInfo ("Authetication", "Keys loaded");
        } 

        public S3Service Connect ()
        {
            /*AmazonS3Config config = CreateConfig ();
            AmazonS3Client connection = (AmazonS3Client)Amazon.AWSClientFactory.CreateAmazonS3Client (, , config);
            return connection;*/

            var s3 = new S3Service
            {
                Host = GlobalSettings.StorageHost,
                AccessKeyID = Credential.PublicKey,
                SecretAccessKey = Credential.SecretKey,
                CustomPort = int.Parse(GlobalSettings.StoragePort),
                UseSsl = true
            };
            return s3;
        }

        public S3Service Reconnect ()
        {
            if (Credential.PublicKey == null || Credential.SecretKey == null) {
                if (Credential.Username == null || Credential.Password == null){
                    return null;
                }
                Authenticate (Credential.Username, Credential.Password);
            }           
            return Connect();
        }

        /*public AmazonS3Config CreateConfig()
        {
            AmazonS3Config conf = new AmazonS3Config();
            conf.ServiceURL =  new Uri(GlobalSettings.StorageURL).Host;
            return conf;
        }*/
    }

}

