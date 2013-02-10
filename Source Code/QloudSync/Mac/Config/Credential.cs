using System;
using System.Configuration;

 namespace GreenQloud
{
	public class Credential : Settings
	{
        private static string username = null;
        public static string Username {
            get{
                if (username==null)
                    username = ConfigurationManager.AppSettings ["Username"];
                return username;
            }
            set{
                username = value;
                AppSettingsUpdate("Username", value);
            }
        }
        
        private static string publickey;
        public static string PublicKey {
            get {
                if(publickey == null)
                    publickey = ConfigurationManager.AppSettings ["PublicKey"];
                return publickey;
            }
            set {
                publickey = value;
                AppSettingsUpdate("PublicKey", value);
            }
        }
        
        private static string secretkey = null;
        public static string SecretKey {
            get {
                if(secretkey == null)
                    secretkey = ConfigurationManager.AppSettings ["SecretKey"];
                return secretkey;
            }
            set {
                secretkey = value;
                AppSettingsUpdate("SecretKey", value);
            }
        }

		public static string Password {
			set;
			get;
		}
		
	}
}

