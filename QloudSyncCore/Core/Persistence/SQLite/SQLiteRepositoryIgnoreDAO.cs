using System;
using System.IO;
using System.Collections.Generic;
using GreenQloud.Model;
using System.Data;

namespace GreenQloud.Persistence.SQLite
{
    public class SQLiteRepositoryIgnoreDAO : RepositoryIgnoreDAO
    {

        #region implemented abstract members of RepositoryDAO

        SQLiteDatabase database = SQLiteDatabase.Instance();

        public override void Create (LocalRepository repo, string path)
        {
            database.ExecuteNonQuery(string.Format("INSERT INTO RepositoryIgnore (RepositoryId, Path) VALUES ('{0}', '{1}')", repo.Id, path));
        }

        public override List<RepositoryIgnore> All(LocalRepository repo)
        {
            return Select(string.Format("SELECT * FROM RepositoryIgnore where RepositoryId = '{0}' ", repo.Id));
        }

        #endregion

        public List<RepositoryIgnore> Select(string sql)
        {
            List<RepositoryIgnore> ignores = new List<RepositoryIgnore>();
            DataTable dt = database.GetDataTable(sql);
            foreach (DataRow dr in dt.Rows)
            {
                RepositoryIgnore e = new RepositoryIgnore(LocalRepository.CreateInstance(int.Parse(dr[1].ToString())));
                e.Id = int.Parse(dr[0].ToString());
                e.Path = dr[2].ToString();
                
                ignores.Add(e);
            }
            return ignores;
        }
    }

}

