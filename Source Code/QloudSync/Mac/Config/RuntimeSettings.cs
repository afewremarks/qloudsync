using System;
using System.Configuration;
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
                return homePath;
            }
        }
        
        public static string TmpPath {
            get {
                return Path.Combine(HomePath, ConfigurationManager.AppSettings ["Tmp"]);
            }
        }
        
        public static string ConfigPath {
            get {
                if (configPath == null)
                    configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), GlobalSettings.ApplicationName);

                if (!Directory.Exists(configPath))
                    Directory.CreateDirectory (configPath);
                return configPath;
            }
        }
        
        public static string BacklogFile {
            get {
                if (backlogFile == null)            
                    backlogFile = Path.Combine (ConfigPath, ConfigurationManager.AppSettings ["BacklogFile"]);
                return backlogFile;
            }
        }
        private static string logfilepath = null;
        public static string LogFilePath {
            get {
                if (logfilepath == null){
                    logfilepath = Path.Combine (ConfigPath, ConfigurationManager.AppSettings ["LogFile"]);
                }
                return logfilepath;
            }
        }

    }
}

