using System;
//using System.Configuration;
using System.IO;

namespace GreenQloud
{
    public class RuntimeSettings : Settings
    {
        private static string homePath = null;
        private static string backlogFile = null;
        private static string configPath = null;


        public static bool FirstRun {
            get {
                return Credential.Username == string.Empty;
            }
        }

        public static string TrashPath { 
            get {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    return Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), GlobalSettings.HomeFolderName);
                else
                    return Path.Combine(Environment.GetFolderPath (Environment.SpecialFolder.Personal), ".trash/");
            }
        }

        public static string HomePath{
            get {
                if (homePath == null)
                {
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                        homePath = Path.Combine(Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), GlobalSettings.HomeFolderName);
                    else
                        homePath = Path.Combine(Environment.GetFolderPath (Environment.SpecialFolder.Personal), GlobalSettings.HomeFolderName);
                }
                if (!homePath.EndsWith (Path.DirectorySeparatorChar.ToString()))
                    homePath += Path.DirectorySeparatorChar;
                return homePath;
            }
        }
        
        /*
        public static string TmpPath {
            get {
                return Path.Combine(HomePath, ConfigFile.GetInstance().Read ("Tmp"));
            }
        }
        */
        public static string ConfigPath {
            get {
                if (configPath == null)
                    configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), GlobalSettings.ApplicationName);

                if (!Directory.Exists(configPath))
                    Directory.CreateDirectory (configPath);
                return configPath;
            }
        }
        
        private static string logfilepath = null;
        public static string LogFilePath {
            get {
                if (logfilepath == null){
                    logfilepath = Path.Combine (ConfigPath, ConfigFile.GetInstance().Read ("LogFile"));
                }
                return logfilepath;
            }
        }

        private static string defaultBucketName = null;
        public static string DefaultBucketName {
            get{
                if (defaultBucketName == null)
                    defaultBucketName = Credential.Username+GlobalSettings.SuffixNameBucket;
                return defaultBucketName;
            }
        }

        public static string DatabaseFolder {
            get{
                return Path.Combine (ConfigPath, ConfigFile.GetInstance().Read ("DatabaseFolder"));
            }
        }

        public static string DatabaseFile {
            get{
                return Path.Combine (DatabaseFolder, ConfigFile.GetInstance().Read ("DatabaseFile"));
            }
        }

        public static string DatabaseVersion {
            get{
                return ConfigFile.GetInstance().Read("DatabaseVersion");
            }
        }
        public static string DatabaseInfoFile {
            get{
                return Path.Combine (DatabaseFolder, ConfigFile.GetInstance().Read("DatabaseInfoFile"));
            }
        }

        public static bool IsLastestVersion
        {
            get
            {
                return (GlobalSettings.RunningVersion == Statistics.VersionAvailable);
            }
        }



        public static string CheckLatestVersionTextUpdate()
        {
            string latestversion = "";
            if (IsLastestVersion)
                latestversion = "This is lastest version available..";
            else
                latestversion = "Lastest version available: " + Statistics.VersionAvailable;

            return latestversion;
        }
    }
}

