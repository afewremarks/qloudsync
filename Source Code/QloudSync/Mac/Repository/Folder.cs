using System;

using System.IO;
using System.Collections.Generic;
using System.Linq;


 namespace GreenQloud.Repository
{
	public class Folder : File
	{

		public Folder (string absolutePath) : base (absolutePath)
		{

		}

		public bool Create ()
		{
			try {
				DirectoryInfo newFolder = new DirectoryInfo (FullLocalName);
				newFolder.Create ();
				return true;
			} catch {
				return false;
			}
		}


		public bool Exists {
			get {
				return new DirectoryInfo (FullLocalName).Exists;
			}
		}		
		
		#region implemented abstract members of QloudBoxObject
		public override string MD5Hash {
            set {
            }
			get {
                return "";
			}
		}
		#endregion

		
		public static List <Folder> Get (List<DirectoryInfo> dirInfos)
		{
			List <Folder> localFiles = new List<Folder>();
			foreach (DirectoryInfo fileInfo in dirInfos)
				localFiles.Add (new Folder (fileInfo.FullName));
			return localFiles;
		}
	}
}

