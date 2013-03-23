using System;
using GreenQloud.Util;
using System.IO;
using GreenQloud.Repository;
using System.Dynamic.Utils;
using Amazon.S3.Model;
using System.Security.Cryptography;
using System.Linq;

namespace GreenQloud.Model
{
    public class RepositoryItem
    {
       

        public RepositoryItem ()
        {
           
        }

        public LocalRepository Repo{
            set; get;
        }

        public string Name { get; set;}
        
        public string RelativePath{ get; set; }

        public bool IsAFolder {
            set;
            get;
        }

        private string fullLocalName = null;
        public string FullLocalName {
            set{
                fullLocalName = value;
            }
            get {
                if(fullLocalName== null)
                    fullLocalName = Repo.Path + AbsolutePath;
                return fullLocalName;
            }
        }
        
        public string FullRemoteName {
            get {
                
                return Path.Combine(RuntimeSettings.DefaultBucketName, AbsolutePath);
            }
        }
        
        public string AbsolutePath{
            get {
                return Path.Combine(RelativePath, Name);
            }
        }
        
        public string RelativePathInBucket {
            get {
                return Path.Combine (RuntimeSettings.DefaultBucketName,RelativePath);
            }
        }
        
        public string TrashFullName {
            get {
                return Path.Combine (TrashRelativePath , Name);
            }
        }
        
        public string TrashRelativePath{
            get{
                return Path.Combine (RuntimeSettings.DefaultBucketName,Constant.TRASH)+RelativePath;
            }
        }
        
        public string TrashAbsolutePath {
            get {
                return Path.Combine (Constant.TRASH,AbsolutePath);
            }
        }
        
        public string MD5Hash {
            set; get;
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
                bool ignored = Constant.EXCLUDE_FILES.Any (s => s == Name) || Name == Constant.CLOCK_TIME || 
                    FullLocalName.Contains(".app/") || Name.EndsWith(".app") || Name=="untitled folder";
                if(File.Exists(FullLocalName))
                {
                    ignored |= (File.GetAttributes(fullLocalName) & FileAttributes.Hidden) == FileAttributes.Hidden;
                }
                else if (Directory.Exists(FullLocalName)){
                    DirectoryInfo d = new DirectoryInfo(FullLocalName);
                    ignored |= (d.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                }

                return ignored;
            }
        }
        
        private string CorrectsDelimiter (string path)
        {
            return path.Replace (Constant.DELIMITER_INVERSE, Constant.DELIMITER);
        }

        string RemoteMD5Hash {
            get;
            set;
        }
       
        public bool IsSync {
            get{
                return MD5Hash == RemoteMD5Hash;
            }
        }

        public long Size {
            get;
            set;
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

