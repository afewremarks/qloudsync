

using System;
using System.Net;
using System.IO;
using System.Text;
using QloudSync.Util;
using QloudSync.Security;




 namespace QloudSync.Net
{
	public class Authentication
	{
		public Authentication ()
		{

		}

        public void Authenticate (string username, string password)
        {
            Uri uri = new Uri(Constant.ADDRESS_TO_AUTHENTICATE);

            WebRequest myReq = WebRequest.Create (uri);
            string usernamePassword = username + ":" + password;
            CredentialCache mycache = new CredentialCache ();
            mycache.Add (uri, "Basic", new NetworkCredential (username, password));
            myReq.Credentials = mycache;
            myReq.Headers.Add ("Authorization", "Basic " + Convert.ToBase64String (new ASCIIEncoding ().GetBytes (usernamePassword)));
            WebResponse wr = myReq.GetResponse ();
            Stream receiveStream = wr.GetResponseStream ();
            StreamReader reader = new StreamReader (receiveStream, Encoding.UTF8);
            Credential.User = username;
            string receiveContent = reader.ReadToEnd ();
            Settings.SecretKey = receiveContent.Substring(Constant.KEY_SECRET_START_INDEX, Constant.KEYS_LENGTH);
            Settings.PublicKey = receiveContent.Substring(Constant.KEY_PUBLIC_START_INDEX, Constant.KEYS_LENGTH);
        
		}
	}
}

