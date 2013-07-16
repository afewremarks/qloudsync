using System;
using GreenQloud.Util;
using System.IO;
using GreenQloud.Repository;
using System.Dynamic.Utils;
using System.Security.Cryptography;
using System.Linq;
using System.Text.RegularExpressions;
using GreenQloud.Persistence;
using GreenQloud.Persistence.SQLite;
using LitS3;

namespace GreenQloud.Model
{
    public class RepositoryItem
    {
        private static readonly SQLiteRepositoryItemDAO dao =  new SQLiteRepositoryItemDAO ();

        public RepositoryItem ()
        {}
        public static RepositoryItem CreateInstance(int id)
        {
            return dao.GetById (id);
        }
        public static RepositoryItem CreateInstance (LocalRepository repo, string key){
            return CreateInstance (repo, key, null, null);
        }

        public static RepositoryItem CreateInstance (LocalRepository repo, string key, string eTag, string localETag){
            RepositoryItem item = new RepositoryItem();
            item.Repository = repo;
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
            RepositoryItem item = new RepositoryItem();
            item.Repository = repo;
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
                return ToPathString(Path.Combine(Repository.Path, Key));
            }
        }

        public string TrashRelativePath {
            get {
                return ToPathString(Path.Combine (Constant.TRASH, Key));
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
            if (IsFolder && !path.EndsWith(Path.DirectorySeparatorChar.ToString())) {
                path += Path.DirectorySeparatorChar;
            }
            path = path.Replace ("/", Path.DirectorySeparatorChar.ToString ());
            return path;
        }
    }
}

