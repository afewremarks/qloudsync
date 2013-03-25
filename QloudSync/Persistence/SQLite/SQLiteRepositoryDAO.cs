using System;
using System.IO;
using System.Collections.Generic;
using GreenQloud.Model;
using System.Linq;
using System.Data;
using Mono.Data.Sqlite;

namespace GreenQloud.Persistence.SQLite
{
    class SQLiteRepositoryDAO : RepositoryDAO
    {

        #region implemented abstract members of RepositoryDAO

        public override void Create (LocalRepository e)
        {
            ExecuteNonQuery (string.Format("INSERT INTO Repository (Path) VALUES (\"{0}\")", e.Path));
        }
        public override List<LocalRepository> All {
            get {        
                return ExecuteReader("SELECT * FROM REPOSITORY");
            }
        }

        public void DeleteAll ()
        {
            ExecuteNonQuery ("DELETE FROM Repository");
        }
        #endregion

        public LocalRepository GetRepositoryByItemFullName (string itemFullName)
        {
            return All.First (r=> itemFullName.Contains(r.Path));
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
        
        public List<LocalRepository> ExecuteReader(string sqlCommand) {
            List<LocalRepository> repos = new List<LocalRepository>();
            using (var conn= new SqliteConnection(SQLiteDatabase.ConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sqlCommand;
                    cmd.CommandType = System.Data.CommandType.Text;
                    SqliteDataReader reader = cmd.ExecuteReader();  
                    while (reader.Read()){
                        repos.Add (new LocalRepository(reader.GetString(1)));
                    }                    
                }
            }
            return repos;
        }  
    }

}

