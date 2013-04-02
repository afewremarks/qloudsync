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
            string sql = string.Format("INSERT INTO REPOSITORYITEM (Name, RelativePath, RepoPath, IsFolder, DELETED) VALUES (\"{0}\", \"{1}\",\"{2}\",\"{3}\",\"{4}\")", item.Name, item.RelativePath, item.Repository.Path, item.IsAFolder, bool.FalseString);
            ExecuteNonQuery (sql);

        }
        public RepositoryItem Create (Event e)
        {
            if (!Exists (e.Item)){
                Create (e.Item);
            }

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
            string sql = string.Format("SELECT * FROM REPOSITORYITEM WHERE NAME = \"{0}\" AND RELATIVEPATH = \"{1}\" AND REPOPATH = \"{2}\"", item.Name, item.RelativePath, item.Repository.Path);
           
            return ExecuteReader(sql).Count != 0 ;
        }



        public void Remove (RepositoryItem item)
        {

            string sql = string.Format("UPDATE REPOSITORYITEM SET DELETED = \"{0}\" WHERE NAME = \"{1}\" AND RELATIVEPATH = \"{2}\" AND REPOPATH = \"{3}\"", bool.TrueString, item.Name, item.RelativePath, item.Repository.Path);
            ExecuteNonQuery (sql);
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
            Console.WriteLine ("ExecuteNonQuery in RepositoryItemDAO");

            SqliteConnection conn= new SqliteConnection(SQLiteDatabase.ConnectionString);
            SqliteCommand cmd = new SqliteCommand();
            try
            {
                conn.Open();
                cmd = conn.CreateCommand();
                
                cmd.CommandText = sqlCommand;
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.ExecuteNonQuery();
                
            }
            catch (Exception e){
                Logger.LogInfo ("Database", e);
            }
            finally{
                
                cmd.Dispose();
                conn.Close();
                conn.Dispose();
                conn = null;
                cmd = null;
                GC.Collect();
            }
        }

        public List<RepositoryItem> ExecuteReader(string sqlCommand) {
            
            Console.WriteLine ("ExecuteReader in RepositoryItemDAO");

            List<RepositoryItem> repos = new List<RepositoryItem>();
            SqliteConnection conn= new SqliteConnection(SQLiteDatabase.ConnectionString);
            SqliteCommand cmd = new SqliteCommand();
            try
            {
                conn.Open();
                cmd = conn.CreateCommand();
                
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
            catch (Exception e){
                Logger.LogInfo ("Database", e);
            }
            finally{
                
                cmd.Dispose();
                conn.Close();
                conn.Dispose();
                conn = null;
                cmd = null;
                GC.Collect();
            }
            return repos;
        } 
    }

}

