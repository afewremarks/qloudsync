using System;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections;

namespace GreenQloud
{

    /* TO DO List
     * in first run, move the config file of bundle to config folder (or create)
     * change all config class to accept Read(key) in replace of AppSettings
     * handle exceptions
     */ 
    public class ConfigFile
    {
        private static string CONFIG_FOLDER = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), GlobalSettings.ApplicationName);
        private static string INIT_CONFIG_FOLDER = AppDomain.CurrentDomain.BaseDirectory;

        private static string INIT_FULLNAME = Path.Combine(INIT_CONFIG_FOLDER, ".." + Path.DirectorySeparatorChar + "Resources" + Path.DirectorySeparatorChar + "qloudsync.config");
        private static string FULLNAME = Path.Combine(CONFIG_FOLDER, "qloudsync.config");


        public ConfigFile ()
        {
        }

        public static void UpdateConfigFile ()
        {
            try{
                if (!File.Exists(FULLNAME)) 
                    File.Copy(INIT_FULLNAME, FULLNAME);
            }catch(Exception e){
                    Logger.LogInfo("Update Config File Error", e);
            }
        }

        public static Hashtable Read(){
            if (File.Exists (FULLNAME)) {
                string[] lines = File.ReadAllLines (FULLNAME);
                Hashtable ht = new Hashtable ();
                foreach(string line in lines){
                    int index = line.IndexOf (":");
                    string key = line.Substring (0, index);
                    string value = line.Substring (index+1, line.Length - index-1);
                    ht [key] = value;
                }
                return ht;
            } else
                throw new ConfigurationException ("Config file do not exists.");
        }

        public static string Read(string key){
            try{
                return Read () [key].ToString();
            }catch{
                throw new ConfigurationException ("Key "+key+" is not found");
            }
        }

        public static bool Write(string key, string value){
            Hashtable properties = Read ();
            properties [key] = value;
            try{
                string texto = "";
                foreach (DictionaryEntry pair in properties){
                    texto+=string.Format("{0}:{1}\n",pair.Key,pair.Value);
                }
                File.WriteAllText(FULLNAME, texto);
            }catch (Exception e){
                throw new ConfigurationException ("Unknown exception: "+e.GetType());
            }
            return true;
        }
    }
}

