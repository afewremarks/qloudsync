using System;
//using System.Configuration;

namespace GreenQloud
{
    public class Preferences : Settings
    {
        public static bool NotificationsEnabled {
            get {
                return bool.Parse (ConfigFile.Read ("NotificationsEnabled"));
            }
            set
            {
                AppSettingsUpdate ("NotificationsEnabled", value.ToString());
            }
        }
    }
}

