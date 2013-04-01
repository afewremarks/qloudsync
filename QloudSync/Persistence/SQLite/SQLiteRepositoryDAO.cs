using System;
using System.IO;
using System.Collections.Generic;
using GreenQloud.Model;
using System.Linq;
using System.Data;
using Mono.Data.Sqlite;

namespace GreenQloud.Persistence.SQLite
{
    public class SQLiteRepositoryDAO : RepositoryDAO
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
            ExecuteNonQuery ("DELETE FROM REPOSITORY");
        }
        #endregion

        public LocalRepository GetRepositoryByItemFullName (string itemFullName)
        {
            LocalRepository repo = All.First (r=> itemFullName.Contains(r.Path));

            if (repo == null)
                return new LocalRepository (RuntimeSettings.HomePath);
            else
                return repo;
        }

        public LocalRepository GetRepositoryByRootName (string root)
        {
            List<LocalRepository> repos = ExecuteReader (string.Format("SELECT * FROM REPOSITORY WHERE PATH LIKE '%{0}'", root));

            if (repos.Count == 0)
                return new LocalRepository (RuntimeSettings.HomePath);
            else
                return repos.First();
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
       
        public List<LocalRepository> ExecuteReader(string sqlCommand) 
        {
            List<LocalRepository> repos = new List<LocalRepository>();
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
                    repos.Add (new LocalRepository(reader.GetString(1)));
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
                if (cmd == null && conn == null)
                    Console.WriteLine ("Disposed");
            }
            return repos;
        }  
    }

}

