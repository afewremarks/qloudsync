using System;
using System.IO;
using System.Collections.Generic;
using GreenQloud.Model;
using System.Linq;
using GreenQloud.Persistence;
using Mono.Data.Sqlite;
using System.Data;

namespace GreenQloud.Persistence.SQLite
{
    class SQLiteRepositoryItemDAO : RepositoryItemDAO
    {
        #region implemented abstract members of RepositoryItemDAO
        SQLiteDatabase database = new SQLiteDatabase();
        public override void Create (RepositoryItem item)
        {
            if (item.ResultItem != null)
                Create (item.ResultItem);

            item.Id = (int) database.ExecuteNonQuery(string.Format("INSERT INTO REPOSITORYITEM (Key, RepositoryId, IsFolder, ResultItemId, eTag, eTagLocal, Moved) VALUES (\"{0}\", \"{1}\",\"{2}\",\"{3}\",'{4}','{5}', '{6}')", item.Key, item.Repository.Id, item.IsFolder, ((item.ResultItem == null) ? "" : item.ResultItem.Id.ToString()), item.ETag, item.LocalETag, item.Moved), true);
        }
        public RepositoryItem Create (Event e)
        {
            if (!Exists (e.Item)) {
                Create (e.Item);
            } else {
                Update (e.Item);
            }

            e.Item.Id = GetId (e.Item);
            return e.Item;
        }


        public override void Update (RepositoryItem i)
        {            
            if (i.ResultItem != null && i.ResultItem.Id == 0)
                Create (i.ResultItem);
            else if(i.ResultItem != null && i.ResultItem.Id != 0)
                 Update (i.ResultItem);
            database.ExecuteNonQuery (string.Format("UPDATE REPOSITORYITEM SET  Key='{1}', RepositoryId='{2}', IsFolder='{3}', ResultItemId = '{4}', eTag = '{5}', eTagLocal = '{6}', Moved = '{7}' WHERE RepositoryItemID =\"{0}\"", i.Id, i.Key, i.Repository.Id, i.IsFolder, ((i.ResultItem == null) ? "" : i.ResultItem.Id.ToString()), i.ETag, i.LocalETag, i.Moved));
        }

        public override List<RepositoryItem> All {
            get {
                return Select("SELECT * FROM REPOSITORYITEM");
            }
        }
        #endregion

        public bool IsFolder (RepositoryItem item)
        {
            if (Exists(item))
            {
                int ID =  GetId (item);
                return GetById (ID).IsFolder;
            }
            return true;
        }

        public bool Exists (RepositoryItem item)
        {
            string sql = string.Format("SELECT * FROM REPOSITORYITEM WHERE Key = \"{0}\" AND RepositoryId = \"{1}\"", item.Key, item.Repository.Id);
            return Select(sql).Count != 0 ;
        }

        public bool ExistsUnmoved (RepositoryItem item)
        {
            string sql = string.Format("SELECT * FROM REPOSITORYITEM WHERE Key = \"{0}\" AND RepositoryId = \"{1}\" AND Moved = '{2}'", item.Key, item.Repository.Id, bool.FalseString);
            return Select(sql).Count != 0 ;
        }

        //TODO FIND By KEy e Repo
        public RepositoryItem GetFomDatabase (RepositoryItem item)
        {
            string sql = string.Format("SELECT * FROM REPOSITORYITEM WHERE Key = \"{0}\" AND RepositoryId = \"{1}\"", item.Key, item.Repository.Id);

            return Select(sql).Last();
        }

        //TODO DONT NEED
        public void Remove (RepositoryItem item)
        {
            //string sql = string.Format("UPDATE REPOSITORYITEM SET DELETED = \"{0}\" WHERE NAME = \"{1}\" AND RELATIVEPATH = \"{2}\" AND REPOPATH = \"{3}\"", bool.TrueString, item.Name, item.RelativePath, item.Repository.Path);
            //database.ExecuteNonQuery (sql);
        }

        //TODO DONT NEED
        public override void MarkAsMoved (RepositoryItem item)
        {
            string sql = string.Format("UPDATE REPOSITORYITEM SET Moved = \"{0}\" WHERE RepositoryItemID = \"{1}\" ", bool.TrueString, item.Id);
            database.ExecuteNonQuery (sql);
        }

        public int GetId (RepositoryItem item)
        {
            if (Exists (item)){
                return Select(string.Format("SELECT * FROM REPOSITORYITEM WHERE  Key = \"{0}\" AND RepositoryId = \"{1}\"", item.Key, item.Repository.Id))[0].Id;
            }
            return 0;
        }

        public RepositoryItem GetById (int id)
        {   

            List<RepositoryItem> items = Select (string.Format("SELECT * FROM REPOSITORYITEM WHERE RepositoryItemID = {0}", id));
            if (items.Count > 0)
                return items.First ();
            return null;
        }

        public List<RepositoryItem> Select (string sql){
            List<RepositoryItem> items = new List<RepositoryItem>();
            DataTable dt = database.GetDataTable(sql);
            foreach(DataRow dr in dt.Rows){
                RepositoryItem item = new RepositoryItem ();
                item.Id = int.Parse(dr[0].ToString());
                item.Key = dr[1].ToString();
                if(dr[2].ToString().Length > 0)
                item.Repository = LocalRepository.CreateInstance(int.Parse(dr[2].ToString()));
                item.IsFolder = bool.Parse (dr[3].ToString());
                if(dr[4].ToString().Length > 0)
                item.ResultItem = RepositoryItem.CreateInstance(int.Parse(dr[4].ToString()));
                item.ETag = dr[5].ToString();
                item.LocalETag = dr[6].ToString();
                item.Moved = bool.Parse (dr[7].ToString());

                items.Add (item);
            }
            return items;
        }
    }

}

