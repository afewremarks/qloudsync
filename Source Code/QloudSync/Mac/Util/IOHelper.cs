using System;
using System.IO;

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
    }
}

