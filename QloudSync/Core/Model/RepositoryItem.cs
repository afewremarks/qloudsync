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
            item.IsFolder = key.EndsWith(Path.DirectorySeparatorChar.ToString());

            if(dao.Exists(item)){
                item = dao.GetFomDatabase (item);
            }
            return item;
        }

        public static RepositoryItem CreateInstance (LocalRepository repo, S3Object s3Object){
            RepositoryItem item = new RepositoryItem();
            item.Repository = repo;
            item.Key = s3Object.Key;
            item.ETag = s3Object.ETag;
            item.LocalETag = null;
            item.IsFolder = s3Object.Key.EndsWith (Path.DirectorySeparatorChar.ToString());

            if(dao.Exists(item)){
                item = dao.GetFomDatabase (item);
            }
            return item;
        }

        public int Id {
            set; get;
        }
        public string Key{
            set; get;
        }

        public string ETag{
            set; get;
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

        public RepositoryItem ResultItem{
            set; get;
        }

        private string localAbsolutePath = null;
        public string LocalAbsolutePath {
            set{
                localAbsolutePath = value;
            }
            get {
                return Path.Combine(Repository.Path, Key);
            }
        }

        public string TrashRelativePath {
            get {
                return Path.Combine (Constant.TRASH, Key);
            }
        }

        public void BuildResultItem(string key){
            ResultItem = CreateInstance (this.Repository, key, this.ETag, this.LocalETag);
        }
    }
}

