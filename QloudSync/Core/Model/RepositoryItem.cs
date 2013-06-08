using System;
using GreenQloud.Util;
using System.IO;
using GreenQloud.Repository;
using System.Dynamic.Utils;
using Amazon.S3.Model;
using System.Security.Cryptography;
using System.Linq;
using System.Text.RegularExpressions;
using GreenQloud.Persistence;
using GreenQloud.Persistence.SQLite;

namespace GreenQloud.Model
{
    public class RepositoryItem
    {
        private static SQLiteRepositoryItemDAO dao =  new SQLiteRepositoryItemDAO ();
        public RepositoryItem ()
        {

        }

        public static RepositoryItem CreateInstance (LocalRepository repo, string fullPath, bool isFolder, long size, string lastModified){
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

        public static RepositoryItem CreateInstance  (LocalRepository repo, string name, string path, bool isFolder, long size, string lastModified){
            RepositoryItem item;
            item = new RepositoryItem();
            item.Repository = repo;
            item.Name = name;
            item.RelativePath = path;
            item.IsAFolder = isFolder;
            item.Size = size;
            item.TimeOfLastChange = lastModified;

            if(dao.Exists(item)){
                item = dao.GetFomDatabase (item);
            }
            return item;
        }

        public LocalRepository Repository{
            set; get;
        }

        private string resultObject = null;
        public string ResultObjectRelativePath {
            set{
                resultObject = value;
            }
            get {
                if (resultObject == null || resultObject == "")
                    return "";
                return ToPathSting(resultObject);
            }
        }

        public string RelativeResultObjectPathInBucket {
            get {

                return ToPathSting( Path.Combine (RuntimeSettings.DefaultBucketName,ResultObjectFolder));
            }
        }

        public string ResultObjectKey {
            get {
                if (resultObject == null || resultObject == ""){
                    return "";
                }else if (resultObject.EndsWith(Path.DirectorySeparatorChar.ToString())){
                    resultObject = resultObject.Substring (0, resultObject.Length - 1);
                }
                return resultObject;
            }
        }

        public string ResultObjectFolder {
            get{
                int i = ResultObjectRelativePath.LastIndexOf (Path.DirectorySeparatorChar);
                return ResultObjectRelativePath.Substring (0, i+1);
            }
        }
        public string ResultObjectName {
            get{
                int i = ResultObjectRelativePath.LastIndexOf (Path.DirectorySeparatorChar);
                return ResultObjectRelativePath.Substring (i+1);
            }
        }

        /*
        public string FullResultObjectName {
            get{
                return Path.Combine(RelativePath, ResultObject);
            }
        }*/
        public string FullLocalResultObject{
            get {
                return ToPathSting(Path.Combine(Repository.Path, ResultObjectRelativePath));
            }
        }

        public string Name { get; set;}

        private string relativePath;
        public string RelativePath{ 
            get{
                return relativePath;
            } 
            set{
                if (value.StartsWith (Path.VolumeSeparatorChar.ToString())) {
                    relativePath = value.Substring (1);
                } else {
                    relativePath = value;
                }
            }
        }

        private string ToPathSting(string path){
            if (IsAFolder && !path.EndsWith(Path.DirectorySeparatorChar.ToString())) {
                path += Path.DirectorySeparatorChar;
            }
            return path;
        }

        public string AbsolutePath{
            get {
                return ToPathSting(Path.Combine(RelativePath, Name));
            }
        }

        public string Key{
            get {
                if(AbsolutePath.EndsWith(Path.VolumeSeparatorChar.ToString())){
                    return AbsolutePath.Substring(0, AbsolutePath.Length - 1);
                } else {
                    return AbsolutePath;
                }
            }
        }

        private string fullLocalName = null;
        public string FullLocalName {
            set{
                fullLocalName = value;
            }
            get {
                return ToPathSting( Path.Combine(Repository.Path, AbsolutePath));
            }
        }
        
        public string FullRemoteName {
            get {
                return ToPathSting( Path.Combine(RuntimeSettings.DefaultBucketName, AbsolutePath));
            }
        }
        
        public string RelativePathInBucket {
            get {
               
                return ToPathSting( Path.Combine (RuntimeSettings.DefaultBucketName,RelativePath));
            }
        }
        
        public string TrashFullName {
            get {
                return ToPathSting( Path.Combine (TrashRelativePath , Name));
            }
        }
        
        public string TrashRelativePath{
            get{
                return ToPathSting( Path.Combine (RuntimeSettings.DefaultBucketName,Constant.TRASH)+RelativePath);
            }
        }
        
        public string TrashAbsolutePath {
            get {
                return ToPathSting( Path.Combine (Constant.TRASH,AbsolutePath));
            }
        }

        public bool IsAFolder {
            set;
            get;
        }
        string remotehash = string.Empty;
        public string RemoteETAG {
            get{
                return remotehash;
            }set{
                remotehash = value;
            }
        }

        string localhash = string.Empty;
        public string LocalETAG {
            get{
                if (localhash == "") {
                    return new Crypto().md5hash(FullLocalName);
                } else {
                    return localhash;
                }
            }set{
                localhash = value;
            }
        }

        public string TimeOfLastChange{
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
                    ignored |= (File.GetAttributes(FullLocalName) & FileAttributes.Hidden) == FileAttributes.Hidden;
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

