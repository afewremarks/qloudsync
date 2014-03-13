
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

        public static string DeviceIdHash {
            get {
                return new Crypto ().md5hash(MachineName+Credential.Username);
            }
        }

        public static string OSVersion {
            get{
                return System.Environment.OSVersion.ToString();
            }
        }

        public static string RunningVersion {
            get {
                return ConfigFile.GetInstance().Read ("RunningVersion");
            }
        }

        public static int IntervalBetweenChecksStatistics{
            get{
                try{
                    return int.Parse(ConfigFile.GetInstance().Read("IntervalBetweenChecksStatistics"));
                }
                catch{
                    Logger.LogInfo ("ERROR ON IntervalBetweenChecksStatistics VALUE", "Invalid IntervalBetweenChecksStatistics value");
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
                return ApplicationName + "; InstanceID: "+ConfigFile.GetInstance().Read("InstanceID");
            }
        }

        public static string HomeFolderName {
            get {
                return ConfigFile.GetInstance().Read("HomeFolderName");
            }
        }

        public static string SuffixNameBucket {
            get{
                return ConfigFile.GetInstance().Read ("SuffixNameBucket");
            }
        }

        public static string AvailableOSXVersion {
            get{
                return ConfigFile.GetInstance().Read ("AvailableOSXVersion");
            }
        }
        
        public static string StorageHost {
            get {
                return ConfigFile.GetInstance().Read ("StorageHost");
            }
        }

        public static string StoragePort {
            get {
                return ConfigFile.GetInstance().Read ("StoragePort");
            }
        }
        
        public static string AuthenticationURL {
            get {
                if (UseAsReseller)
                    return ConfigFile.GetInstance().Read("AuthenticationURLReseller");
                else
                    return ConfigFile.GetInstance().Read ("AuthenticationURL");
            }
        }

        public static bool UseAsReseller
        {
            get
            {
                return bool.Parse(ConfigFile.GetInstance().Read("UseAsReseller"));
            }
        }
        
        public static string Trash {
            get {
                if (trash == null)
                    trash = ConfigFile.GetInstance().Read ("Trash");
                return trash;
            }
        }

        public static int UploadTimeout {
            get {
                try{
                return int.Parse(ConfigFile.GetInstance().Read ("UploadTimeout"));
                }
                catch{
                    Logger.LogInfo ("ERROR INVALID GlobalSettings", "Invalid UploadTimeout value");
                    return 3600000;
                }
            }
        }

        
        public static int IntervalBetweenChecksRemoteRepository {
            get {
                try{
                    return int.Parse(ConfigFile.GetInstance().Read ("IntervalBetweenChecksRemoteRepository"));
                }
                catch{
                    Logger.LogInfo ("ERROR INVALID GlobalSettings", "Invalid IntervalBetweenChecksRemoteRepository value");
                    return 30000;
                }
            }
        }


    }
}

