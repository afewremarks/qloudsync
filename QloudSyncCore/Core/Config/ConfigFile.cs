using System;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections;

namespace GreenQloud
{

    public class ConfigFile : AbstractConfigFile<ConfigFile>
    {
        protected string INIT_CONFIG_FOLDER;
        protected string INIT_FULLNAME;

        public ConfigFile(){
            INIT_CONFIG_FOLDER = AppDomain.CurrentDomain.BaseDirectory;
            INIT_FULLNAME = Path.Combine(INIT_CONFIG_FOLDER, ".." + Path.DirectorySeparatorChar.ToString() + "Resources" + Path.DirectorySeparatorChar.ToString() + "qloudsync.conf");
            FULLNAME = Path.Combine(CONFIG_FOLDER, "qloudsync.conf");
        }

        public override void UpdateConfigFile ()
        {
            if(!File.Exists(INIT_FULLNAME)){
                INIT_FULLNAME = Path.Combine(INIT_CONFIG_FOLDER, "qloudsync.conf");
            }

            try{
                if (!File.Exists(FULLNAME)) 
                    File.Copy(INIT_FULLNAME, FULLNAME);
            }catch(Exception e){
                Logger.LogInfo("ERROR ON UPDATE CONFIG FILE", e);
            }
        }
    }
}

