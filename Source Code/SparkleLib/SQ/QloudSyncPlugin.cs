using System;

using System.Linq;
using System.Windows;
using System.Windows.Forms;

using System.IO;
using System.Collections.Generic;
using  SQ.Net.S3;
using  SQ.Net;
using  SQ.Security;
using SQ.Util;
using SQ.Repository;


namespace SQ
{
	public class QloudSyncPlugin
	{

		public static bool Connect ()
		{
			if (!LoadCredentials ()) 
				return false;

			return RemoteRepo.Connected;
		}
	

		private static bool LoadCredentials ()
		{
			try{
                FileInfo keyFile = new FileInfo (Constant.KEY_FILE);
                if (keyFile.Exists){
                    StreamReader sr = keyFile.OpenText();
                    Credential.User = sr.ReadLine();
                    Credential.PublicKey = sr.ReadLine();
                    Credential.SecretKey = sr.ReadLine ();
                    sr.Close();
                    return true;
                }
                return false;
			}
			catch{
				return false;
			}
		}

		public static void WriteKeys ()
        {
			try {
				FileInfo file = new FileInfo (Constant.KEY_FILE);
				if (file.Exists)
					file.Attributes = FileAttributes.Normal;
                StreamWriter fileStream = new StreamWriter (Constant.KEY_FILE);
				fileStream.WriteLine (Credential.User);
				fileStream.WriteLine (Credential.PublicKey);
				fileStream.WriteLine (Credential.SecretKey);
				fileStream.WriteLine (Credential.URLConnection);
				fileStream.Close ();
				file.Attributes = FileAttributes.Hidden;
			} catch {
                Console.WriteLine("Unable to write file "+Constant.KEY_FILE);
			}
		}

        public static bool InitRepo{
            set; get;
        }
		
		/*private static string GetConnectionUrl()
		{
            string url = Config.GetUrlForFolder(new DirectoryInfo(GetFolderRepo).Name);
			//string url ="";
			url = url.Substring(0, url.LastIndexOf('/'));
			return new Uri(url).Host;
		}*/

	}
}