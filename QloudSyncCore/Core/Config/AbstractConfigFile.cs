using System;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections;

namespace GreenQloud
{

    public abstract class AbstractConfigFile<T>
    {
        protected string CONFIG_FOLDER = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), GlobalSettings.ApplicationName);
		protected Hashtable ht = new Hashtable ();
        protected object _readLock = new object();
		protected static T instance;

		protected string FULLNAME {
			get;
			set;
		}

		public static T GetInstance(){
			if (instance == null)
				instance = Activator.CreateInstance<T> ();
			return instance;
		}

		public abstract void UpdateConfigFile();

        public virtual Hashtable Read(){

            lock (_readLock) {
                if (ht.Count > 0)
                    return ht;

				if (File.Exists (FULLNAME)) {
					string[] lines = File.ReadAllLines (FULLNAME);
					foreach (string line in lines) {
						int index = line.IndexOf (":");
						string key = line.Substring (0, index);
						string value = line.Substring (index + 1, line.Length - index - 1);
						ht [key] = value;
					}
					return ht;
				} else {
					throw new ConfigurationException ("Config file ["+ FULLNAME +"] do not exists.");
				}
            }
        }

        public virtual string Read(string key){
            try{
                return Read () [key].ToString();
            }catch{
                throw new ConfigurationException ("Key "+key+" is not found");
            }
        }

        public virtual bool Write(string key, string value){
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

