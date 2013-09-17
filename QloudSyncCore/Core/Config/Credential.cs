using System;
//using System.Configuration;
using System.Xml;
using System.IO;

 namespace GreenQloud
{
	public class Credential 
    {

        private static XmlCredential xml = new XmlCredential();

        public static string Username {
            set {
                if(value != null)
                    xml.Username = value.ToLower();
            }
            get {
                return xml.Username;
            }
        }

        public static string PublicKey {
            set {
                xml.PublicKey = value;
            }
            get {
                return xml.PublicKey;
            }
        }        

        public static string SecretKey {
            set {
                xml.SecretKey = value;
            }
            get {
                return xml.SecretKey;
            }
        }

        public static string Password {
            set {
                xml.Password = value;
            }
            get {
                return xml.Password;
            }
        }

        class XmlCredential: XmlDocument
	    {
            string credential_path = Path.Combine(RuntimeSettings.ConfigPath, "credentials.xml");

            private void Create (){
                if(!System.IO.File.Exists (credential_path))
                    System.IO.File.WriteAllText (credential_path,
                                                 "<?xml version=\"1.0\" encoding=\"UTF-8\" ?>\n<credential>\n</credential>");
                Load (credential_path);
            }
            
            private void Save ()
            {
                Save (credential_path);                
            }

            private void SetCredentialInfo (string credential_info, string value)
            {       
                Create ();

                XmlNode node_root = SelectSingleNode ("/credential");
                XmlNode node_credential_info = node_root.SelectSingleNode (credential_info);
                if (node_credential_info == null) {
                    node_credential_info = CreateElement (credential_info);
                    node_root.AppendChild (node_credential_info);
                }

                node_credential_info.InnerText       = value;

                Save ();
            }

            private string GetCredentialInfo (string credential_info)
            {
                Create ();
                XmlNode node = SelectSingleNode(string.Format("/credential/{0}", credential_info));
                if (node == null)
                    return string.Empty;
                else
                    return node.InnerText;
            }
           

            private string username = null;
            public string Username {
                get{
                    if (username==null)
                        username = GetCredentialInfo ("Username");
                    return username;
                }
                set{
                    username = value;
                    SetCredentialInfo("Username", value);
                }
            }
            
            private string publickey;
            public string PublicKey {
                get {
                    if(publickey == null)
                        publickey = GetCredentialInfo ("PublicKey");
                    return publickey;
                }
                set {
                    publickey = value;
                    SetCredentialInfo("PublicKey", value);
                }
            }
            
            private string secretkey = null;
            public string SecretKey {
                get {
                    if(secretkey == null)
                        secretkey = GetCredentialInfo ("SecretKey");
                    return secretkey;
                }
                set {
                    secretkey = value;
                    SetCredentialInfo("SecretKey", value);
                }
            }

    		public string Password {
    			set;
    			get;
    		}
        }
	}
}

