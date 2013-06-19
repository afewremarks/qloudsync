using System;
using GreenQloud;
using System.Net;
using System.IO;
using System.Text;
using LitS3;

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

        public S3Service Connect ()
        {
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
    }

}

