using System;
using System.Collections.Generic;
using  SQ.Synchrony;
using System.IO;
using System.Linq;
using SQ.Util;

namespace SQ.Repository
{
    public class LocalRepo
    {
		private static List<File> files = null;

        private LocalRepo ()
        {
        }

        public static string LocalFolder {
            set;
            get;
        }

        public static List<File> Files {
			set {
				files = value;
			}
			get {
				if(files==null)
					files = GetFiles();
				return files;
			}
		}

		public static List<File> GetFiles ()
		{
			try {
				System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo (LocalFolder);
				List<File> list = LocalFile.Get (dir.GetFiles ("*", System.IO.SearchOption.AllDirectories).ToList ());
				list.AddRange (Folder.Get (dir.GetDirectories ("*", System.IO.SearchOption.AllDirectories).ToList ()));

				return list;
			} catch (System.ArgumentNullException) {
				Logger.LogInfo("Error", "Set a LocalFolder variable");
				return null;
			}
		}

        public static List<Folder> WatchedFolders{ set; get;}

        public static List<Change> PendingChanges{ set; get; }

        public static List<Sync> Syncs{ set; get; }

        public static List<Folder> EmptyFolders {
            get {
                List<DirectoryInfo> emptyDirectories = new DirectoryInfo (LocalFolder)
                .GetDirectories ("*", SearchOption.AllDirectories)
                    .Where (d => !Directory.EnumerateFileSystemEntries (d.FullName).Any ()).ToList ();
                List<Folder> emptyFolders = new List<Folder> ();
                foreach (DirectoryInfo dir in emptyDirectories) 
                    emptyFolders.Add (new Folder (dir.FullName + Constant.DELIMITER));
                return emptyFolders;
            }
        }

        public static double Size {
			get {
				return GetFolderSize (LocalFolder);
			}
        }

		private static double GetFolderSize (string folderName)
		{
			double size = 0;

			if(!new DirectoryInfo(folderName).Exists)
				return 0;
			foreach (string fileName in Directory.GetFiles (folderName, "*.*")) 
				if (!new LocalFile(fileName).IsIgnoreFile && System.IO.File.Exists(fileName))
					size += new FileInfo (fileName).Length;
			if(!new DirectoryInfo(folderName).Exists)
				return 0;
			foreach (string dirName in Directory.GetDirectories (folderName, "*.*")) 
				size += GetFolderSize (dirName);

			
			return size;
		}

        public static string ResolveDecodingProblem (string path)
        {
            bool haveProblem = false;
            string old = char.ConvertFromUtf32(97)+""+char.ConvertFromUtf32(769);
            string _new = char.ConvertFromUtf32(225).ToString();
            if (!path.Contains (old)) return path;
            char[] chars = path.ToCharArray ();
            for (int  c = 0; c < chars.Count()-1; c++) {
                if ((int)chars [c] == 97 && (int)chars [c + 1] == 769) {    
                    haveProblem = true;
                    break;
                }
            }
            
            if (haveProblem) {
                path = path.Replace (old,_new);
            }
            return path;
        }
    }
}

