using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using GreenQloud.Repository;
using GreenQloud.Util;
using LitS3;
using System.Linq;
using GreenQloud.Persistence.SQLite;

namespace GreenQloud.Model
{

    public enum ItemType {
        DEFAULT,
        IMAGE,
        TEXT,
        VIDEO,
        AUDIO,
        FOLDER
    } 

    public class RepositoryItem
    {
        private static readonly SQLiteRepositoryItemDAO dao = new SQLiteRepositoryItemDAO();

        public RepositoryItem (LocalRepository repo)
        {
            this.Repository = repo;
        }
        public static RepositoryItem CreateInstance(int id)
        {
            return dao.GetById (id);
        }
        public static RepositoryItem CreateInstance (LocalRepository repo, string key){
            key = key.Replace(Path.DirectorySeparatorChar.ToString(), "/"); 
            return CreateInstance (repo, key, null, null);
        }

        public static RepositoryItem CreateInstance (LocalRepository repo, string key, string eTag, string localETag){
            key = key.Replace(Path.DirectorySeparatorChar.ToString(), "/");
            RepositoryItem item = new RepositoryItem(repo);
            item.Key = key;
            item.ETag = eTag;
            item.LocalETag = localETag;
            item.IsFolder = key.EndsWith("/");

            if(dao.ExistsUnmoved(item)){
                item = dao.GetFomDatabase (item);
            }
            return item;
        }

        public static RepositoryItem CreateInstance (LocalRepository repo, ListEntry entry){
            RepositoryItem item = new RepositoryItem(repo);
            string key = "";
            if (entry is CommonPrefix) {
                key = ((CommonPrefix)entry).Prefix;
            } else {
                key = ((ObjectEntry)entry).Key;
            }
            item.Key = key;
            item.LocalETag = null;
            item.IsFolder = key.EndsWith("/");

            if(dao.ExistsUnmoved(item)){
                item = dao.GetFomDatabase (item);
            }
            return item;
        }

        public int Id {
            set; get;
        }

        public int ResultItemId {
            set; get;
        }

        public string Key{
            set; get;
        }

        public bool Moved {
            get;
            set;
        }

        public string UpdatedAt {
            get;
            set;
        }

        public string Name{
            get {
               return Key.Substring (Key.LastIndexOf("/")+1);
            }
        }

        public ItemType Type {
            get {
                if (IsFolder)
                    return ItemType.FOLDER;

                string name = Name;
                string extension = "";
                if(name.IndexOf(".") >= 0)
                    extension = name.Substring (Name.LastIndexOf (".")+1);

                string[] images =  new string[7] {"png" , "jpg", "gif", "jpeg", "tiff", "bmp", "JPG"};
                string[] text =  new string[6] {"pdf", "doc", "docx", "odf", "txt", "xls"};
                string[] video =  new string[7] {"mp4" , "m4v", "ogg", "webm", "mov","avi", "midi"};
                string[] audio =  new string[3] {"mp3", "m4a", "wav"};

                if(images.Contains(extension))
                    return ItemType.IMAGE;
                if(text.Contains(extension))
                    return ItemType.TEXT;
                if(video.Contains(extension))
                    return ItemType.VIDEO;
                if(audio.Contains(extension))
                    return ItemType.AUDIO;



                return ItemType.DEFAULT;
            }
        }

        private string etag;
        public string ETag{
            set{
                etag = value;
            } 
            get{
                if (etag == null)
                    return null;
                return etag.Replace ("\"", "");
            }
        }

        public string LocalETag{
            set; get;
        }

        public bool IsFolder{
            set; get;
        }

        public LocalRepository Repository{
            set; get;
        }

        private RepositoryItem resultItem;
        public RepositoryItem ResultItem{
            set {
                resultItem = value;
                if (resultItem != null)
                    ResultItemId = resultItem.Id;
            }
            get {
                if (resultItem == null && ResultItemId > 0)
                    resultItem = CreateInstance (ResultItemId);
                return resultItem;
            }
        }

        public string LocalAbsolutePath {
            get {
                return ToPathString(Path.Combine(Repository.Path, KeyRelative));
            }
        }

        public string LocalFolderPath {
            get {
                string path = "";
                path = Path.Combine (Repository.Path, KeyRelative.Replace("/",Path.DirectorySeparatorChar.ToString()));
                path = path.Substring(0, path.LastIndexOf(Path.DirectorySeparatorChar.ToString())); 
                return ToPathString (path);
            }
        }

        public string KeyRelative {
            get {
                return Key.Substring (Repository.RemoteFolder.Length);
            }
        }

        public string RemoteTrashPath {
            get {
                string keyReturn = ToPathString(Path.Combine (Constant.TRASH, Key));
                keyReturn = keyReturn.Replace(Path.DirectorySeparatorChar.ToString(), "/");

                RemoteRepositoryController remoteController = new RemoteRepositoryController(this.Repository);
                int i = 0;
                string modifiedKeyReturn = keyReturn;
                while (remoteController.Exists(modifiedKeyReturn))
                {
                    i++;
                    int folderDelimiter = keyReturn.LastIndexOf("/");
                    int extensionDelimiter = Name.LastIndexOf(".");
                    if (folderDelimiter == keyReturn.Count() - 1) {//if true, its a folder
                        modifiedKeyReturn = keyReturn.Insert(folderDelimiter, " (" + i + ")");
                    }
                    else //its a file
                    {
                        if (extensionDelimiter != -1 && extensionDelimiter != 0)
                        { //file have extension
                            modifiedKeyReturn = keyReturn.Insert(keyReturn.LastIndexOf("."), " (" + i + ")");
                        }
                        else 
                        {
                            modifiedKeyReturn = keyReturn + " (" + i + ")";
                        }
                    }
                } 

                return modifiedKeyReturn;
            }
        }

        public void BuildResultItem(string key){
            if(key != string.Empty) {
                resultItem = CreateInstance (this.Repository, key, this.ETag, this.LocalETag);
                ResultItemId = resultItem.Id;
            }
        }

        private string ToPathString (string path)
        {
            path = path.Replace("/", Path.DirectorySeparatorChar.ToString());
            if (IsFolder && !path.EndsWith(Path.DirectorySeparatorChar.ToString())) {
                path += Path.DirectorySeparatorChar;
            }
            return path;
        }

    }
}

