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
            CreateRepositoryTable();
            CreateRepositoryItemTable();
            CreateEventTable();
            CreateTransferTable();
        }

        static void CreateRepositoryTable ()
        {
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
        //(Name, RelativePath, RepoPath)
        static void CreateRepositoryItemTable ()
        {
            using (var conn= new SqliteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "CREATE TABLE RepositoryItem (RepositoryItemID INTEGER PRIMARY KEY AUTOINCREMENT , Name ntext, RelativePath ntext, RepoPath ntext, IsFolder ntext)";
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.ExecuteNonQuery();
                }
            }
        }
        //(ITEMID, TYPE, REPOSITORY, SYNCHRONIZED, INSERTTIME)
        static void CreateEventTable ()
        {
            using (var conn= new SqliteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "CREATE TABLE EVENT (EventID INTEGER PRIMARY KEY AUTOINCREMENT , ItemId INTEGER, TYPE ntext, REPOSITORY ntext, SYNCHRONIZED ntext, INSERTTIME ntext)";
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        //(ITEMID, INITIALTIME, ENDTIME, TYPE, STATUS)
        static void CreateTransferTable ()
        {
            using (var conn= new SqliteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "CREATE TABLE TRANSFER (TransferID INTEGER PRIMARY KEY AUTOINCREMENT , ItemId INTEGER, INITIALTIME ntext, ENDTIME ntext, TYPE ntext, STATUS ntext)";
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}

