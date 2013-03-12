using System;
using System.IO;
using System.Collections.Generic;

namespace GreenQloud
{
    public class IOHelper
    {
        public IOHelper ()
        {
        }
        public static void CreateParentFolders (string path)
        {
            int lastIndex = path.LastIndexOf("/");
            path = path.Substring(0, lastIndex);
            
            if (!Directory.Exists(path))
            {
                CreateParentFolders (path);
                Directory.CreateDirectory(path);
            }
        }

        private static List<string> deletedfolders = null;
        public static List<string> DeleteParentFolders (string path)
        {
            deletedfolders = new List<string>();
            DeleteParent(path);
            return deletedfolders;
        }

        protected static void DeleteParent (string path)
        {
            int lastIndex = path.LastIndexOf ("/");
            path = path.Substring (0, lastIndex);

            if (path != RuntimeSettings.HomePath) {
                if (Directory.Exists(path)){
                    bool hasfolders = Directory.GetDirectories(path).Length != 0;
                    if( !hasfolders && Directory.GetFiles(path).Length == 0){
                        Directory.Delete(path);
                        deletedfolders.Add(path);
                    }
                    else if (!hasfolders && Directory.GetFiles(path).Length == 1){
                        string fileName = Directory.GetFiles(path)[0];
                        if( fileName== ".DS_Store"){
                            File.Delete(Path.Combine(path, fileName));
                            Directory.Delete(path);
                            deletedfolders.Add(path);
                        }
                    }
                }
            }
            else
                return;
            DeleteParentFolders (path);
        }
    }
}

