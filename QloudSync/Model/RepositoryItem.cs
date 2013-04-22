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

        public static RepositoryItem CreateInstance (LocalRepository repo, string fullPath, bool isFolder, long size, DateTime lastModified){
            if (fullPath.EndsWith ("/")){
                fullPath = fullPath.Substring (0, fullPath.Length-1);
                isFolder = true;
            }

            if (!fullPath.StartsWith("/"))
                fullPath = "/"+fullPath;

            if (fullPath.Contains (Constant.TRASH))
                fullPath =  fullPath.Replace (Constant.TRASH, string.Empty);

            int lastIndexDelimiter = fullPath.LastIndexOf ("/");

            string name = fullPath.Substring (lastIndexDelimiter+1, fullPath.Length-lastIndexDelimiter-1);
            string repoPath = repo.Path;

            if (!repoPath.EndsWith ("/"))
                repoPath += "/";
            string relativePath = "";
            if (name == string.Empty)
            {
                //TODO
            }else if(repoPath == string.Empty){
                relativePath = fullPath.Replace (RuntimeSettings.HomePath, string.Empty).Replace(name, string.Empty);
            }else {
                relativePath = fullPath.Replace (repoPath, string.Empty).Replace(name, string.Empty);
            }
            if (relativePath == "/")
                relativePath = "";
            if (relativePath.StartsWith ("/"))
                relativePath = relativePath.Substring (1, relativePath.Length-1);
            if (relativePath.EndsWith ("/"))
                relativePath = relativePath.Substring (0, relativePath.Length-1);

            return CreateInstance (repo, name, relativePath, isFolder, size, lastModified);
        }

        public static RepositoryItem CreateInstance  (LocalRepository repo, string name, string path, bool isFolder, long size, DateTime lastModified){
            RepositoryItem item = new RepositoryItem();
            item.Repository = repo;
            item.Name = name;
            item.RelativePath = path;
            item.IsAFolder = isFolder;
            item.Size = size;
            item.TimeOfLastChange = lastModified;
            return item;
        }

        public LocalRepository Repository{
            set; get;
        }

        public string Name { get; set;}

        public string RelativePath{ 
            get; set;
        }

        public string AbsolutePath{
            get {
                return Path.Combine(RelativePath, Name);
            }
        }

        private string fullLocalName = null;
        public string FullLocalName {
            set{
                fullLocalName = value;
            }
            get {
                if(fullLocalName == null){                    

                    fullLocalName = Path.Combine(Repository.Path, AbsolutePath);

                }
                

                return fullLocalName;
            }
        }
        
        public string FullRemoteName {
            get {
                
                return Path.Combine(RuntimeSettings.DefaultBucketName, AbsolutePath);
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

        public bool IsAFolder {
            set;
            get;
        }
        string remotehash = string.Empty;
        public string RemoteMD5Hash {
            get{
                return remotehash;
            }set{
                remotehash = value;
            }
        }   

        string localhash = string.Empty;
        public string LocalMD5Hash {
            get{
                return localhash;
            }set{
                localhash = value;
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
        
        public string Id {
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

