using System;
using Mono.Data.Sqlite;
using System.IO;

namespace GreenQloud.Persistence.SQLite
{

    public class SQLiteDatabase
    {
        public static string PathToDataBase{
            get{
                string databaseName = "QloudSync.db";
                string folder = Path.Combine(RuntimeSettings.ConfigPath, "Db");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory (folder);
                return Path.Combine(folder, databaseName);
            }
        }

        public static string ConnectionString{
            get{
                return String.Format("URI=file:{0};Version=3;", PathToDataBase);;
            }
        }
        
        public static void CreateDataBase(){
            using (var conn= new SqliteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "CREATE TABLE Repository (RepositoryID INTEGER PRIMARY KEY AUTOINCREMENT , Path ntext)";
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.ExecuteNonQuery();
                }
            }
        }

    }
}

