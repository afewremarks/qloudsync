using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using QloudSync.Util;


 namespace QloudSync.Repository
{
    public abstract class File
    {


        public File (string absolutePath)
        {
			InTrash = false;
            MakePaths (absolutePath);
        }

        void MakePaths (string absolutePath)
		{

			if (absolutePath.Contains (Constant.TRASH)) {
				InTrash = true;
				absolutePath = absolutePath.Replace (Constant.TRASH, ""); 
			}



           absolutePath = absolutePath.Replace (RuntimeSettings.HomePath, "");
            
            if (absolutePath.Contains (Constant.DELIMITER)) {
                
                string tempPath = absolutePath;
                int lastDelimiterIndex = 0;

                while (tempPath.EndsWith (Constant.DELIMITER))
                {
                    tempPath = tempPath.Substring (0, tempPath.Length - 1);
                }
                if (tempPath.Contains (Constant.DELIMITER))
                {
                    lastDelimiterIndex = tempPath.LastIndexOf (Constant.DELIMITER)+1;
                }

                Name = absolutePath.Substring (lastDelimiterIndex, absolutePath.Length - lastDelimiterIndex);

				RelativePath = absolutePath.Replace (Name, "").Replace ("//",Constant.DELIMITER);
                if (RelativePath.StartsWith(Constant.DELIMITER))
                    RelativePath = RelativePath.Substring (1, RelativePath.Length-1);
                if (RelativePath == Constant.DELIMITER)
                    RelativePath = "";

                
            } else {
                Name = absolutePath;
                RelativePath = "";
            }
        }
        
        public string Name { get; private set;}
        
        public string RelativePath{ get; private set; }
        
        public string FullLocalName {
            get {
                    return Path.Combine(RuntimeSettings.HomePath,AbsolutePath);
            }
        }
        
        public string FullRemoteName {
            get {
                
                return Path.Combine(RemoteRepo.DefaultBucketName,AbsolutePath);
            }
        }
        
        public string AbsolutePath{
            get {
                return Path.Combine(RelativePath, Name);
            }
        }
        
        public string RelativePathInBucket {
            get {
                return Path.Combine (RemoteRepo.DefaultBucketName,RelativePath);
            }
        }
        
        public string TrashFullName {
            get {
                return Path.Combine (TrashRelativePath , Name);
            }
        }
        
        public string TrashRelativePath{
            get{
                return Path.Combine (RemoteRepo.DefaultBucketName,Constant.TRASH)+RelativePath;
            }
        }

		public string TrashAbsolutePath {
			get {
                return Path.Combine (Constant.TRASH,AbsolutePath);
			}
		}
        
        public File RecentVersion {
            get;
            set;
        }
        
        public File OldVersion{
            set;
            get;
        }
        
        public abstract string MD5Hash{
            get; set;
        }

        public DateTime TimeOfLastChange{
            set;
            get;
        }

        public bool Deleted{
            set;
            get;
        }
        
        public static File Get (File  SQObject, List<File>  SQObjectList)
        {
            return  SQObjectList.Where (obj => obj.RelativePath ==  SQObject.RelativePath && obj.Name ==  SQObject.Name).SingleOrDefault ();
        }
        
        public bool ExistsInLocalRepo {
            get {
                if (IsAFolder)
                    return Directory.Exists(FullLocalName);
                else
                    return System.IO.File.Exists(FullLocalName);
            }
            
        }
        
        public bool InTrash {
			set; get;         
        }
        
        public bool IsAFolder {
            get {
               
                bool isfolder = Directory.Exists (FullLocalName);

                if (isfolder)
				{
					if(!Name.EndsWith (Constant.DELIMITER))
                    Name = Name+Constant.DELIMITER;
				}
                return Name.EndsWith(Constant.DELIMITER);
            }
        }
        
        public bool IsIgnoreFile {
            get {
				if(IsAFolder){
					return false;
				}
                return Constant.EXCLUDE_FILES.Where (s => s == Name) .Any() || Name == Constant.CLOCK_TIME || FullLocalName.Contains(".app/") || Name.EndsWith(".app");
            }
        }

        private string CorrectsDelimiter (string path)
        {
            return path.Replace (Constant.DELIMITER_INVERSE, Constant.DELIMITER);
        }
    }
}

