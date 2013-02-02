
using  SQ.Security;

using System;
using System.Net;
using System.IO;
using System.Text;
using SQ.Util;


namespace SQ.Net
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
            RecoverKeys(reader.ReadToEnd ());
        }


		private void RecoverKeys(string receiveContent)
		{
			//FIXME The base authentication response is JSON format. Doing a substring using an index will brake. Fix by using a JSON parser. 
			Credential.SecretKey = receiveContent.Substring(Constant.KEY_SECRET_START_INDEX, Constant.KEYS_LENGTH);
			Credential.PublicKey = receiveContent.Substring(Constant.KEY_PUBLIC_START_INDEX, Constant.KEYS_LENGTH);
		}
	}
}

