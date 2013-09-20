using System;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections;

namespace GreenQloud
{

    public class SelectedFoldersConfig : AbstractConfigFile<SelectedFoldersConfig>
    {
        public SelectedFoldersConfig(){
            FULLNAME = Path.Combine(CONFIG_FOLDER, "selected_folders.config");
        }
		
        public override void UpdateConfigFile ()
        {
            try{
                if (!File.Exists(FULLNAME)) 
                    File.Create(FULLNAME);
            }catch(Exception e){
                    Logger.LogInfo("Update selective folders config file error", e);
            }
        }
    }
}

