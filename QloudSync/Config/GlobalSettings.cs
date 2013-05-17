
using System;
//using System.Configuration;

namespace GreenQloud
{
    public class GlobalSettings : Settings
    {
        private static string trash = null;

        public static string MachineName {
            get{
                return System.Environment.MachineName;
            }
        }

        public static string OSVersion {
            get{
                return System.Environment.OSVersion.ToString();
            }
        }

        public static string RunningVersion {
            get {
                return ConfigFile.Read ("RunningVersion");
            }
        }

        public static int IntervalBetweenChecksStatistics{
            get{
                try{
                    return int.Parse(ConfigFile.Read("IntervalBetweenChecksStatistics"));
                }
                catch{
                    Logger.LogInfo ("GlobalSettings", "Invalid IntervalBetweenChecksStatistics value");
                    return 3600000;
                }
            }
        }

        public static string ApplicationName {
            get {
                return "QloudSync";
            }
        }
        public static string FullApplicationName {
            get {
                return ApplicationName + "; InstanceID: "+ConfigFile.Read("InstanceID");
            }
        }

        public static string HomeFolderName {
            get {
                return ConfigFile.Read("HomeFolderName");
            }
        }

        public static string SuffixNameBucket {
            get{
                return ConfigFile.Read ("SuffixNameBucket");
            }
        }

        public static string AvailableOSXVersion {
            get{
                return ConfigFile.Read ("AvailableOSXVersion");
            }
        }
        
        public static string StorageURL {
            get {
                return ConfigFile.Read ("StorageURL");
            }
        }
        
        public static string AuthenticationURL {
            get {
                return ConfigFile.Read ("AuthenticationURL");
            }
        }
        
        public static string Trash {
            get {
                if (trash == null)
                    trash = ConfigFile.Read ("Trash");
                return trash;
            }
        }

        public static int UploadTimeout {
            get {
                try{
                return int.Parse(ConfigFile.Read ("UploadTimeout"));
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
                    return int.Parse(ConfigFile.Read ("IntervalBetweenChecksRemoteRepository"));
                }
                catch{
                    Logger.LogInfo ("GlobalSettings", "Invalid IntervalBetweenChecksRemoteRepository value");
                    return 30000;
                }
            }
        }


    }
}

