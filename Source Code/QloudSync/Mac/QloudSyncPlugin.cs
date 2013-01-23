using System;

using System.Linq;
using System.Windows;
using System.Windows.Forms;

using System.IO;
using System.Collections.Generic;
using QloudSync.Repository;
using QloudSync.Util;
using QloudSync.Security;



 namespace QloudSync
{
	public class QloudSyncPlugin
	{

		

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