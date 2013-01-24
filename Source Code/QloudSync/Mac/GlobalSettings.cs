using System;
using System.Configuration;

namespace QloudSync
{
    public class GlobalSettings : Settings
    {
        private static string trash = null;

        public static string ApplicationName {
            get {
                return ConfigurationManager.AppSettings ["ApplicationName"];
            }
        }
        
        public static string SuffixNameBucket {
            get{
                return ConfigurationManager.AppSettings ["SuffixNameBucket"];
            }
        }
        
        public static string StorageURL {
            get {
                return ConfigurationManager.AppSettings ["StorageURL"];
            }
        }
        
        public static string AuthenticationURL {
            get {
                return ConfigurationManager.AppSettings ["AuthenticationURL"];
            }
        }
        
        public static string Trash {
            get {
                if (trash == null)
                    trash = ConfigurationManager.AppSettings ["Trash"];
                return trash;
            }
        }

    }
}

