//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program.  If not, see <http://www.gnu.org/licenses/>.


using System;
using System.Configuration;
using System.IO;


namespace QloudSync {

    public class Settings{
        private static string homePath = null;
        private static string backlogFile = null;
        private static string configPath = null;
        private static string trash = null;
        private Settings(){

        }

        #region Pre-defined

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

        #endregion

        #region RunTime
        public static bool FirstRun {
            get {
                return ConfigurationManager.AppSettings ["Username"] == null;
            }
        }

        public static string HomePath{
            get {
                if (homePath == null)
                {
                    if (SparkleBackend.Platform == PlatformID.Win32NT)
                        homePath = Path.Combine(Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), ApplicationName);
                    else
                        homePath = Path.Combine(Environment.GetFolderPath (Environment.SpecialFolder.Personal), ApplicationName);
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
                    configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ApplicationName);
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
                if (logfilepath == null)
                    logfilepath = Path.Combine (configPath, ConfigurationManager.AppSettings ["LogFile"]);
                return logfilepath;
            }
        }

     
        #endregion

        #region User prefferences

        public static bool NotificationsEnabled {
            get {
                return bool.Parse (ConfigurationManager.AppSettings ["NotificationsEnabled"]);
            }
            set
            {
                ConfigurationManager.AppSettings["NotificationsEnabled"] = value.ToString();
            }
        }

        #endregion

        #region User informations
        private static string username = null;
        public static string Username {
            get{
                if (username==null)
                    username = ConfigurationManager.AppSettings ["Username"];
                return username;
            }
            set{
                username = value;
                ConfigurationManager.AppSettings ["Username"] = value;
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
                ConfigurationManager.AppSettings ["PublicKey"] = value;
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
                ConfigurationManager.AppSettings ["SecretKey"] = value;
            }
        }
        #endregion
    }
}
