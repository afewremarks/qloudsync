using System;
using System.Collections.Generic;

using System.IO;
using System.Linq;
using GreenQloud.Synchrony;
using GreenQloud.Util;
using MonoMac.AppKit;


 namespace GreenQloud.Repository
{
    public class LocalRepo
    {
		private static List<StorageQloudObject> files = null;

        private LocalRepo ()
        {
        }
       

        public static List<StorageQloudObject> Files {
			set {
				files = value;
			}
			get {
				if(files==null)
                    files = GetSQObjects(RuntimeSettings.HomePath);
				return files;
			}
		}


		public static List<StorageQloudObject> GetSQObjects (string path)
		{
			try {

                System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo (path);
                List<StorageQloudObject> list = new List<StorageQloudObject>();
                if(dir.Exists){
                    foreach (FileInfo fileInfo in dir.GetFiles ("*", System.IO.SearchOption.AllDirectories).ToList ()) {
                        StorageQloudObject localFile = new StorageQloudObject (fileInfo.FullName, fileInfo.LastWriteTime);
                        if(!localFile.IsIgnoreFile)
                            list.Add (localFile);
                    }

                    foreach (DirectoryInfo fileInfo in dir.GetDirectories ("*", System.IO.SearchOption.AllDirectories).ToList ())
                        list.Add (new StorageQloudObject (fileInfo.FullName));
                }
				return list;
			} catch (Exception e) {
                Logger.LogInfo ("Error", "Fail to load local files");
				Logger.LogInfo("Error", e);
				return null;
			}
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

        public static void Delete(StorageQloudObject sqObj){
            if(Directory.Exists (sqObj.FullLocalName)){
                try{
                    List<StorageQloudObject> sqObjectsInFolder = GetSQObjects(sqObj.FullLocalName);
                    string path = Path.Combine(RuntimeSettings.TrashPath, sqObj.Name);

                    if(Directory.Exists(path)){
                        Directory.Move (path, path+" "+DateTime.Now.ToString("dd.mm.ss tt"));
                    }else{
                        Directory.Move (sqObj.FullLocalName, path);
                        RemoveFromLists (sqObj);
                    }

                    foreach (StorageQloudObject s in sqObjectsInFolder)
                    {
                        RemoveFromLists (s);
                    }
                }
                catch {
                    Logger.LogInfo ("Error", string.Format("Fail to delete folder \"{0}\" in local repo.", sqObj.FullLocalName));
                }
            }
            else if (File.Exists (sqObj.FullLocalName)){
                try{
                    string path = Path.Combine(RuntimeSettings.TrashPath, sqObj.Name);

                    if(File.Exists (path)){
                        string newpath =  path+" "+DateTime.Now.ToString ("dd.mm.ss tt");
                        File.Move (path, newpath);
                    }

                    CreatePath (path);
                    File.Move(sqObj.FullLocalName, path);
                    RemoveFromLists (sqObj);
                }
                catch (Exception e){
                    Logger.LogInfo ("Error", string.Format("Fail to delete file \"{0}\" in local repo.", sqObj.FullLocalName));
                    Logger.LogInfo ("Error", e);
                }
            }
        }

        static void RemoveFromLists (StorageQloudObject sqObj)
        {
            Files.Remove (sqObj);
            BacklogSynchronizer.GetInstance().RemoveFileByAbsolutePath (sqObj);
        }

       
        public static bool Exists (StorageQloudObject remoteFile)
        {
            return System.IO.File.Exists (remoteFile.FullLocalName);
        }

        public static void CreateFolder (StorageQloudObject folder)
        {
            if(!Directory.Exists(folder.FullLocalName)){
                CreatePath (folder.FullLocalName);
                Directory.CreateDirectory(folder.FullLocalName);
                Files.Add(folder);
                BacklogSynchronizer.GetInstance().AddFile (folder);
            }
        }

        public static void CreatePath (string path)
        {
            string parent = path.Substring (0,path.LastIndexOf("/"));

            if (path.EndsWith ("/")) {
                parent = parent.Substring (0, parent.LastIndexOf("/"));
            }
            if (parent == string.Empty)
                return;

            CreatePath(parent);
           
            if(!Directory.Exists(parent))
                Directory.CreateDirectory(parent);
        }
    }
}

