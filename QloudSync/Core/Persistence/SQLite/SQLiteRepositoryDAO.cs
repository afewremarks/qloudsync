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
        SQLiteDatabase database = new SQLiteDatabase ();
        public override void Create (LocalRepository e)
        {
            database.ExecuteNonQuery (string.Format("INSERT INTO Repository (Path) VALUES (\"{0}\")", e.Path));
        }
        public override List<LocalRepository> All {
            get {        
              return Select("SELECT * FROM REPOSITORY");
            }
        }

        public void DeleteAll ()
        {
            database.ExecuteNonQuery ("DELETE FROM REPOSITORY");
        }
        #endregion

        public LocalRepository GetRepositoryByItemFullName (string itemFullName)
        {
            LocalRepository repo = All.First (r=> itemFullName.StartsWith(r.Path));

            if (repo == null)
                return new LocalRepository (RuntimeSettings.HomePath);
            else
                return repo;
        }

        public LocalRepository GetRepositoryByRootName (string root)
        {
            List<LocalRepository> repos = Select(string.Format("SELECT * FROM REPOSITORY WHERE PATH LIKE '%{0}'", root));

            if (repos.Count == 0)
                return new LocalRepository (RuntimeSettings.HomePath);
            else
                return repos.First();
        }


        public List<LocalRepository> Select (string sql){
            List<LocalRepository> repos = new List<LocalRepository>();
            DataTable dt = database.GetDataTable(sql);
            foreach(DataRow dr in dt.Rows){
                repos.Add (new LocalRepository(dr[1].ToString()));
            }
            return repos;
        }
    }

}

