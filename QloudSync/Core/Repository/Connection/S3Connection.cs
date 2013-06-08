using System;
using Amazon.S3;
using GreenQloud;
using System.Net;
using System.IO;
using System.Text;

namespace QloudSync.Repository
{
    public class S3Connection
    {
        public S3Connection ()
        {
        }

        public static void Authenticate (string username, string password)
        {
            Uri uri = new Uri (GlobalSettings.AuthenticationURL);

            WebRequest myReq = WebRequest.Create (uri);
            string usernamePassword = username + ":" + password;
            CredentialCache mycache = new CredentialCache ();
            mycache.Add (uri, "Basic", new NetworkCredential (username, password));
            myReq.Credentials = mycache;
            myReq.Headers.Add ("Authorization", "Basic " + Convert.ToBase64String (new ASCIIEncoding ().GetBytes (usernamePassword)));
            using (WebResponse wr = myReq.GetResponse ()){
                Stream receiveStream = wr.GetResponseStream ();
                StreamReader reader = new StreamReader (receiveStream, Encoding.UTF8);
                string receiveContent = reader.ReadToEnd ();
                Newtonsoft.Json.Linq.JObject o = Newtonsoft.Json.Linq.JObject.Parse(receiveContent);               
                Credential.SecretKey = (string)o["api_private_key"];
                Credential.PublicKey = (string)o["api_public_key"];
            }
            Logger.LogInfo ("Authetication", "Keys loaded");
        } 

        public AmazonS3Client Connect ()
        {
            AmazonS3Config config = CreateConfig ();
            AmazonS3Client connection = (AmazonS3Client)Amazon.AWSClientFactory.CreateAmazonS3Client (Credential.PublicKey, Credential.SecretKey, config);
            return connection;
        }

        public AmazonS3Client Reconnect ()
        {
            if (Credential.PublicKey == null || Credential.SecretKey == null) {
                if (Credential.Username == null || Credential.Password == null){
                    return null;
                }
                Authenticate (Credential.Username, Credential.Password);
            }           
            return Connect();
        }

        public AmazonS3Config CreateConfig()
        {
            AmazonS3Config conf = new AmazonS3Config();
            conf.ServiceURL =  new Uri(GlobalSettings.StorageURL).Host;
            return conf;
        }
    }

}

