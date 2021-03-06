using System;
using System.IO;
using System.Collections.Generic;
using GreenQloud.Model;
using System.Linq;
using GreenQloud.Persistence;
using System.Data;

namespace GreenQloud.Persistence.SQLite
{
    public class SQLiteRepositoryItemDAO : RepositoryItemDAO
    {
        #region implemented abstract members of RepositoryItemDAO

        SQLiteDatabase database = SQLiteDatabase.Instance();

        public RepositoryItem Create (Event e)
        {
            if (!ExistsUnmoved (e.Item)) {
                Create (e.Item);
            } else {
                Update (e.Item);
            }
            if (e.Item.ResultItem != null && e.Item.ResultItem.Id == 0) {
                Create (e.Item.ResultItem);
                e.Item.ResultItemId = GetId ( e.Item.ResultItem);
                Update (e.Item);
            }else if(e.Item.ResultItem != null && e.Item.ResultItem.Id != 0)
                Update (e.Item.ResultItem);

            e.Item.Id = GetId (e.Item);
            return e.Item;
        }

        public override void Create (RepositoryItem item)
        {
            item.Id = (int)database.ExecuteNonQuery(string.Format("INSERT INTO REPOSITORYITEM (RepositoryItemKey, RepositoryId, IsFolder, ResultItemId, eTag, eTagLocal, Moved, UpdatedAt) VALUES ('{0}', '{1}','{2}','{3}','{4}','{5}', '{6}', '{7}')", item.Key.Replace("'", "''"), item.Repository.Id, item.IsFolder, ((item.ResultItem == null) ? "" : item.ResultItemId.ToString()), item.ETag, item.LocalETag, item.Moved, item.UpdatedAt), true);
        }


        public override void Update (RepositoryItem i)
        {
            database.ExecuteNonQuery(string.Format("UPDATE REPOSITORYITEM SET  RepositoryItemKey='{1}', RepositoryId='{2}', IsFolder='{3}', ResultItemId = '{4}', eTag = '{5}', eTagLocal = '{6}', Moved = '{7}', UpdatedAt = '{8}' WHERE RepositoryItemID ='{0}'", i.Id, i.Key.Replace("'", "''"), i.Repository.Id, i.IsFolder, ((i.ResultItem == null) ? "" : i.ResultItemId.ToString()), i.ETag, i.LocalETag, i.Moved, i.UpdatedAt));
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
            string sql = string.Format("SELECT count(*) FROM REPOSITORYITEM WHERE RepositoryItemKey = '{0}'", item.Key.Replace("'", "''"));
            int i = int.Parse(database.ExecuteScalar (sql));
            return i > 0;
        }

        public bool ExistsUnmoved (RepositoryItem item)
        {
            return ExistsUnmoved(item.Key, item.Repository);
        }

        public RepositoryItem GetFomDatabase (RepositoryItem item)
        {
            string sql = string.Format("SELECT * FROM REPOSITORYITEM WHERE RepositoryItemKey = '{0}'", item.Key.Replace("'", "''"));
            return Select(sql).Last();
        }

        public bool ExistsUnmoved(string key, LocalRepository repo)
        {
            string sql = string.Format("SELECT COUNT(*) FROM REPOSITORYITEM WHERE RepositoryItemKey = '{0}' AND Moved <> '{1}'", key.Replace("'", "''"), bool.TrueString);
            return int.Parse(database.ExecuteScalar(sql)) > 0;
        }

        //TODO DONT NEED
        public void Remove (RepositoryItem item)
        {
            //string sql = string.Format("UPDATE REPOSITORYITEM SET DELETED = '{0}' WHERE NAME = '{1}' AND RELATIVEPATH = '{2}' AND REPOPATH = '{3}'", bool.TrueString, item.Name, item.RelativePath, item.Repository.Path);
            //database.ExecuteNonQuery (sql);
        }

        public override void MarkAsMoved (RepositoryItem item)
        {
            item.Moved = true;
            string sql;
            if (item.IsFolder)
            {
                sql = string.Format("UPDATE REPOSITORYITEM SET Moved = '{0}' WHERE RepositoryId = '{1}' AND RepositoryItemKey = '{2}'", bool.TrueString, item.Repository.Id, item.Key.Replace("'", "''"), item.Id);
                database.ExecuteNonQuery(sql);
                sql = string.Format("UPDATE REPOSITORYITEM SET Moved = '{0}' WHERE RepositoryId = '{1}' AND RepositoryItemKey <> '{2}' AND RepositoryItemKey LIKE '{2}%' ", bool.TrueString, item.Repository.Id, item.Key.Replace("'", "''"));
                database.ExecuteNonQuery(sql);
            }
            else
            {
                sql = string.Format("UPDATE REPOSITORYITEM SET Moved = '{0}' WHERE RepositoryId = '{1}' AND RepositoryItemKey = '{2}' ", bool.TrueString, item.Repository.Id, item.Key.Replace("'", "''"));
                database.ExecuteNonQuery(sql);
            }
            
        }

        public override void ActualizeUpdatedAt (RepositoryItem item){
            string sql = string.Format("UPDATE REPOSITORYITEM SET UpdatedAt = '{0}' WHERE RepositoryItemID = '{1}' ", item.UpdatedAt, item.Id);
            database.ExecuteNonQuery (sql);
        }
        public override void UpdateETAG (RepositoryItem item){
            string sql = string.Format("UPDATE REPOSITORYITEM SET  eTag = '{0}', eTagLocal = '{1}' WHERE RepositoryItemID = '{2}' ", item.ETag, item.LocalETag, item.Id);
            database.ExecuteNonQuery (sql);
        }
        public override void MarkAsUnmoved (RepositoryItem item)
        {
            item.Moved = true;
            string sql;
            if(item.IsFolder)
                sql = string.Format("UPDATE REPOSITORYITEM SET Moved = '{0}' WHERE RepositoryItemID = '{1}' AND RepositoryItemKey LIKE '{2}%' ", bool.FalseString, item.Id, item.Key.Replace("'", "''"));
            else
                sql = string.Format("UPDATE REPOSITORYITEM SET Moved = '{0}' WHERE RepositoryItemID = '{1}' ", bool.FalseString, item.Id);
            database.ExecuteNonQuery (sql);
        }
        public int GetId (RepositoryItem item)
        {
            if (Exists (item)){
                //if (item.Id != 0)
                //    return item.Id;
                //else
                return Select(string.Format("SELECT * FROM REPOSITORYITEM WHERE  RepositoryItemKey = '{0}' AND RepositoryId = '{1}'", item.Key.Replace("'", "''"), item.Repository.Id)).Last().Id;
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
                RepositoryItem item = new RepositoryItem (LocalRepository.CreateInstance(int.Parse(dr[2].ToString())));
                item.Id = int.Parse(dr[0].ToString());
                item.Key = dr[1].ToString();
                if(dr[2].ToString().Length > 0)
                item.IsFolder = bool.Parse (dr[3].ToString());
                if(dr[4].ToString().Length > 0)
                item.ResultItemId = int.Parse (dr[4].ToString());
                item.ETag = dr[5].ToString();
                item.LocalETag = dr[6].ToString();
                item.Moved = bool.Parse (dr[7].ToString());
                if(dr[8].ToString().Length > 0)
                item.UpdatedAt = dr[8].ToString();

                items.Add (item);
            }
            return items;
        }
    }

}

