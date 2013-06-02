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
            database.ExecuteNonQuery(string.Format("INSERT INTO REPOSITORYITEM (Name, RelativePath, RepoPath, IsFolder, DELETED, eTag, ResultObject) VALUES (\"{0}\", \"{1}\",\"{2}\",\"{3}\",\"{4}\",'{5}', \"{6}\")", item.Name, item.RelativePath, item.Repository.Path, item.IsAFolder, bool.FalseString, item.RemoteETAG, item.ResultObject));

        }
        public RepositoryItem Create (Event e)
        {
            if (!Exists (e.Item)){
                Create (e.Item);
            }

            e.Item.Id = GetId (e.Item);
            return e.Item;
        }


        public override void Update (RepositoryItem i)
        {            
            database.ExecuteNonQuery (string.Format("UPDATE REPOSITORYITEM SET  ResultObject = \"{1}\", eTag = '{2}' WHERE RepositoryItemID =\"{0}\"", i.Id, i.ResultObject, i.RemoteETAG));
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
                string ID =  GetId (item);
                return GetById (int.Parse(ID)).IsAFolder;
            }
            return true;
        }

        public bool Exists (RepositoryItem item)
        {
            string sql = string.Format("SELECT * FROM REPOSITORYITEM WHERE NAME = \"{0}\"  AND RELATIVEPATH = \"{1}\" AND REPOPATH = \"{2}\"", item.Name, item.RelativePath, item.Repository.Path);
           
            return Select(sql).Count != 0 ;
        }

        public RepositoryItem GetFomDatabase (RepositoryItem item)
        {
            string sql = string.Format("SELECT * FROM REPOSITORYITEM WHERE NAME = \"{0}\"  AND RELATIVEPATH = \"{1}\" AND REPOPATH = \"{2}\"", item.Name, item.RelativePath, item.Repository.Path);

            return Select(sql).Last();
        }

        public void Remove (RepositoryItem item)
        {

            string sql = string.Format("UPDATE REPOSITORYITEM SET DELETED = \"{0}\" WHERE NAME = \"{1}\" AND RELATIVEPATH = \"{2}\" AND REPOPATH = \"{3}\"", bool.TrueString, item.Name, item.RelativePath, item.Repository.Path);
            database.ExecuteNonQuery (sql);
        }

        public string GetId (RepositoryItem item)
        {

            if (Exists (item)){
                return Select(string.Format("SELECT * FROM REPOSITORYITEM WHERE  NAME = \"{0}\" AND RELATIVEPATH = \"{1}\" AND REPOPATH = \"{2}\"", item.Name,  item.RelativePath, item.Repository.Path))[0].Id;
            }
            return "";
        }

        public RepositoryItem GetById (int id)
        {   

            RepositoryItem item = Select(string.Format("SELECT * FROM REPOSITORYITEM WHERE RepositoryItemID = {0}", id))[0];

            return item;
        }

        public List<RepositoryItem> Select (string sql){
            List<RepositoryItem> items = new List<RepositoryItem>();
            DataTable dt = database.GetDataTable(sql);
            foreach(DataRow dr in dt.Rows){
                RepositoryItem item = new RepositoryItem ();
                item.Id = dr[0].ToString();
                item.Name = dr[1].ToString();
                item.RelativePath = dr[2].ToString();
                item.Repository = new LocalRepository(dr[3].ToString());
                item.IsAFolder = bool.Parse (dr[4].ToString());
                item.ResultObject = dr[6].ToString();
                item.RemoteETAG = dr[7].ToString();

                items.Add (item);
            }
            return items;
        }
    }

}

