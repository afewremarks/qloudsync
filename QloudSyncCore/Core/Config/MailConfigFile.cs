using System;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections;

namespace GreenQloud
{

    public class MailConfigFile : AbstractConfigFile<MailConfigFile>
    {
        protected string INIT_CONFIG_FOLDER;
        protected string INIT_FULLNAME;

        public MailConfigFile(){
            INIT_CONFIG_FOLDER = AppDomain.CurrentDomain.BaseDirectory;
            INIT_FULLNAME = Path.Combine(INIT_CONFIG_FOLDER, ".." + Path.DirectorySeparatorChar.ToString() + "Resources" + Path.DirectorySeparatorChar.ToString() + "qloudsync_mail.conf");
            FULLNAME = Path.Combine(CONFIG_FOLDER, "qloudsync_mail.conf");
        }

        public override void UpdateConfigFile ()
        {
            if(!File.Exists(INIT_FULLNAME)){
                INIT_FULLNAME = Path.Combine(INIT_CONFIG_FOLDER, "qloudsync_mail.conf");
            }

            try{
                if (!File.Exists(FULLNAME)) 
                    File.Copy(INIT_FULLNAME, FULLNAME);
            }catch(Exception e){
                    Logger.LogInfo("Update Config File Error", e);
            }
        }
    }
}

