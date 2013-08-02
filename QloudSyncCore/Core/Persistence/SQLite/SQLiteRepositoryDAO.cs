using System;
using System.IO;
using System.Collections.Generic;
using GreenQloud.Model;
using System.Linq;
using System.Data;

namespace GreenQloud.Persistence.SQLite
{
    public class SQLiteRepositoryDAO : RepositoryDAO
    {

        #region implemented abstract members of RepositoryDAO

        SQLiteDatabase database = new SQLiteDatabase();

        public override void Create (LocalRepository e)
        {
            database.ExecuteNonQuery (string.Format("INSERT INTO Repository (Path, RECOVERING) VALUES (\"{0}\", \"{1}\")", e.Path, e.Recovering.ToString()));
        }

        public void Update (LocalRepository repo)
        {
            database.ExecuteNonQuery (string.Format("UPDATE Repository SET Path=\"{0}\", RECOVERING=\"{1}\" WHERE RepositoryID='{2}'", repo.Path, repo.Recovering.ToString(), repo.Id));
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
            return All.First (r=> itemFullName.StartsWith(r.Path));
        }

        public LocalRepository FindOrCreateByRootName (string root)
        {
            List<LocalRepository> repos = Select(string.Format("SELECT * FROM REPOSITORY WHERE PATH == '{0}'", root));
            LocalRepository repo = repos.First ();
            if (repo == null){
                repo = new LocalRepository ();
                repo.Path = root;
                Create (repo);
                return FindOrCreateByRootName (root);
            }
            return repos.First();
        }

        public LocalRepository GetById (int id)
        {
            List<LocalRepository> repos = Select(string.Format("SELECT * FROM REPOSITORY WHERE RepositoryID = '{0}'", id));
            return repos.First();
        }


        public List<LocalRepository> Select (string sql){
            List<LocalRepository> repos = new List<LocalRepository>();
            DataTable dt = database.GetDataTable(sql);
            foreach(DataRow dr in dt.Rows){
                LocalRepository r = new LocalRepository ();
                r.Id = int.Parse (dr[0].ToString());
                r.Path = dr [1].ToString ();
                if(dr[2].ToString().Length > 0)
                    r.Recovering = bool.Parse (dr[2].ToString());
                repos.Add (r);
            }
            return repos;
        }
    }

}

