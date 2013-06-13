using System;
using GreenQloud.Util;
using System.IO;
using GreenQloud.Repository;
using Amazon.S3.Model;
using System.Security.Cryptography;
using System.Linq;

namespace GreenQloud
{
    public class StorageQloudObject
    {
        public bool IsAFolder {
            get {
                if(AsS3Object != null)
                    return AsS3Object.Key.EndsWith("/");
                else
                {
                    if (Directory.Exists(FullLocalName))
                        return true;
                    else
                        return false;
                }
            }

        }

        public S3Object AsS3Object {
            get;
            set;
        }

 

        public StorageQloudObject (S3Object s3Object)
        {
            AsS3Object = s3Object;
            MakePaths (AsS3Object.Key);
        }

        public StorageQloudObject (string absolutePath)
        {
            AsS3Object = null;
            InTrash = false;
            MakePaths (absolutePath);
        }

        public StorageQloudObject (string absolutePath, DateTime timeOfLastChange)
        {
            AsS3Object = null;
            InTrash = false;
            TimeOfLastChange = timeOfLastChange;
            MakePaths (absolutePath);
        }
        
        void MakePaths (string absolutePath)
        {
            
            if (absolutePath.Contains (Constant.TRASH)) {
                InTrash = true;
                absolutePath = absolutePath.Replace (Constant.TRASH, ""); 
            }
            
            absolutePath = absolutePath.Replace (RuntimeSettings.HomePath, "");
            
            if (absolutePath.Contains ("\\")) {
                
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
                
                RelativePath = absolutePath.Replace (Name, "").Replace ("\\",Constant.DELIMITER);
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

        private string fullLocalName = null;
        public string FullLocalName {
            set{
                fullLocalName = value;
            }
            get {
                if(fullLocalName== null)
                    fullLocalName = Path.Combine(RuntimeSettings.HomePath,AbsolutePath);
                return fullLocalName;
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
        
        public string LocalMD5Hash {
            get {
                string md5hash;
                try {       
                    FileStream fs = System.IO.File.Open (FullLocalName, FileMode.Open);
                    MD5 md5 = MD5.Create ();
                    md5hash = BitConverter.ToString (md5.ComputeHash (fs)).Replace (@"-", @"").ToLower ();
                    fs.Close ();
                } catch{
                    md5hash = string.Empty;
                }
                return md5hash;
            }
        }

        public string RemoteMD5Hash {
            get {
                return AsS3Object.ETag.Replace("\"","");
            }
        }
        
        public DateTime TimeOfLastChange{
            set;
            get;
        }
        
        public bool Deleted{
            set;
            get;
        }
        
        public int Id {
            set;
            get;
        }       

        public bool InTrash {
            set; get;         
        }
        
        public bool IsIgnoreFile {
            get {               
                return Constant.EXCLUDE_FILES.Any (s => s == Name) || Name == Constant.CLOCK_TIME || FullLocalName.Contains(".app/") || Name.EndsWith(".app");
            }
        }
        
        private string CorrectsDelimiter (string path)
        {
            return path.Replace (Constant.DELIMITER_INVERSE, Constant.DELIMITER);
        }       
       
        public bool IsSync {
            get{
                return LocalMD5Hash == RemoteMD5Hash;
            }
        }
        
        protected bool IsFileLocked {
            get {
                FileStream stream = null;
                
                try {
                    FileInfo file = new FileInfo (FullLocalName);
                    if (file.Exists)
                        stream = file.Open (FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    else
                        return false;
                } catch (IOException) {
                    return true;
                } finally {
                    if (stream != null)
                        stream.Close ();
                }
                return false;
            }
        }
    }
}

