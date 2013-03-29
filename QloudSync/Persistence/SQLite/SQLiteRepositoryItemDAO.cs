using System;
using System.IO;
using System.Collections.Generic;
using GreenQloud.Model;
using System.Linq;
using GreenQloud.Persistence;
using Mono.Data.Sqlite;

namespace GreenQloud.Persistence.SQLite
{
    class SQLiteRepositoryItemDAO : RepositoryItemDAO
    {
        #region implemented abstract members of RepositoryItemDAO
        public override void Create (RepositoryItem item)
        {
            string sql = string.Format("INSERT INTO REPOSITORYITEM (Name, RelativePath, RepoPath, IsFolder) VALUES (\"{0}\", \"{1}\",\"{2}\",\"{3}\")", item.Name, item.RelativePath, item.Repository.Path, item.IsAFolder);
            ExecuteNonQuery (sql);
        }

        public RepositoryItem Create (Event e)
        {
            if (!Exists (e.Item))
                Create (e.Item);

            e.Item.Id = GetId (e.Item);
            return e.Item;
        }


        public override List<RepositoryItem> All {
            get {
                return ExecuteReader ("SELECT * FROM REPOSITORYITEM");
            }
        }
        #endregion

        public bool Exists (RepositoryItem item)
        {
            return All.Exists (i => i.AbsolutePath == item.AbsolutePath && i.Repository.Path == item.Repository.Path);
        }



        public void Remove (RepositoryItem item)
        {
            ExecuteNonQuery (string.Format("DELETE REPOSITORYITEM WHERE NAME = \"{0}\" AND RELATIVEPATH = \"{1}\" AND REPOPATH = \"{2}\"", item.Name, item.RelativePath, item.Repository.Path));
        }

        public string GetId (RepositoryItem item)
        {
            if (Exists (item)){
                return ExecuteReader (string.Format("SELECT * FROM REPOSITORYITEM WHERE NAME = \"{0}\" AND RELATIVEPATH = \"{1}\" AND REPOPATH = \"{2}\"", item.Name, item.RelativePath, item.Repository.Path))[0].Id;
            }
            return "";
        }

        public RepositoryItem GetById (int id)
        {   
            RepositoryItem item = ExecuteReader (string.Format("SELECT * FROM REPOSITORYITEM WHERE RepositoryItemID = {0}", id))[0];

            return item;
        }

        public void ExecuteNonQuery (string sqlCommand)
        {
            using (var conn= new SqliteConnection(SQLiteDatabase.ConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sqlCommand;
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.ExecuteNonQuery();
                }
            }
        }



        public List<RepositoryItem> ExecuteReader(string sqlCommand) {
            List<RepositoryItem> repos = new List<RepositoryItem>();
            using (var conn= new SqliteConnection(SQLiteDatabase.ConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sqlCommand;
                    cmd.CommandType = System.Data.CommandType.Text;
                    SqliteDataReader reader = cmd.ExecuteReader();  
                    while (reader.Read()){
                        RepositoryItem item = new RepositoryItem ();
                        item.Id = reader.GetInt32 (0).ToString();
                        item.Name = reader.GetString(1);
                        item.RelativePath = reader.GetString(2);
                        item.Repository = new LocalRepository(reader.GetString(3));
                        item.IsAFolder = bool.Parse (reader.GetString (4));
                        repos.Add (item);
                    }                    
                }
            }
            return repos;
        } 
    }

}

