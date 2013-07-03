using System;
using System.IO;
using System.Collections.Generic;
using GreenQloud.Model;
using System.Linq;
using System.Data;
using Mono.Data.Sqlite;

namespace GreenQloud.Persistence.SQLite
{
    public class SQLiteTimeDiffDAO : TimeDiffDAO
    {

        #region implemented abstract members of RepositoryDAO
        SQLiteDatabase database = new SQLiteDatabase ();
        public override void Create (double e)
        {
            database.ExecuteNonQuery (string.Format("INSERT INTO TimeDiff (Diff) VALUES ('{0}')", e));
        }
        public override double Last {
            get {        
                return Select("SELECT Diff FROM TimeDiff ORDER BY TimeDiffID DESC LIMIT 1");
            }
        }
        public override double Count {
            get {        
                return int.Parse(database.ExecuteScalar("SELECT COUNT(*) FROM TimeDiff"));
            }
        }
        #endregion

        public double Select (string sql){
            DataTable dt = database.GetDataTable(sql);
            return double.Parse(dt.Rows[0][0].ToString());
        }
    }

}

