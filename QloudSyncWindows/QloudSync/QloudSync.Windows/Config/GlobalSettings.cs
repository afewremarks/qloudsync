using System;
using System.Configuration;

namespace GreenQloud
{
    public class GlobalSettings : Settings
    {
        private static string trash = null;

        public static string RunningVersion {
            get {
                return ConfigurationManager.AppSettings ["RunningVersion"];
            }
        }

        public static int IntervalBetweenChecksStatistics{
            get{
                try{
                    return int.Parse(ConfigurationManager.AppSettings ["IntervalBetweenChecksStatistics"]);
                }
                catch{
                    Logger.LogInfo ("GlobalSettings", "Invalid IntervalBetweenChecksStatistics value");
                    return 3600000;
                }
            }
        }

        public static string ApplicationName {
            get {
                return ConfigurationManager.AppSettings ["ApplicationName"];
            }
        }

        public static string HomeFolderName {
            get {
                return ConfigurationManager.AppSettings ["HomeFolderName"];
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

        public static int UploadTimeout {
            get {
                try{
                return int.Parse(ConfigurationManager.AppSettings ["UploadTimeout"]);
                }
                catch{
                    Logger.LogInfo ("GlobalSettings", "Invalid UploadTimeout value");
                    return 3600000;
                }
            }
        }

        
        public static int IntervalBetweenChecksRemoteRepository {
            get {
                try{
                    return int.Parse(ConfigurationManager.AppSettings ["IntervalBetweenChecksRemoteRepository"]);
                }
                catch{
                    Logger.LogInfo ("GlobalSettings", "Invalid IntervalBetweenChecksRemoteRepository value");
                    return 30000;
                }
            }
        }
    }
}

