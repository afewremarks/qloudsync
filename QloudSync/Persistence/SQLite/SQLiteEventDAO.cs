using System;
using GreenQloud.Repository.Remote;
using GreenQloud.Repository;
using GreenQloud.Persistence;
using GreenQloud.Repository.Local;
using GreenQloud.Model;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using GreenQloud.Persistence.SQLite;
using System.Linq;

namespace GreenQloud.Persistence
{
	public class SQLiteEventDAO : EventDAO
	{

        SQLiteRepositoryItemDAO repositoryItemDAO = new SQLiteRepositoryItemDAO();
        #region implemented abstract members of EventDAO

        public override void Create (Event e)
        {
            if (e == null)
                return;
            e.Item = repositoryItemDAO.Create (e);
           
           if (NotExistsConflict(e)){
                string sql =string.Format("INSERT INTO EVENT (ITEMID, TYPE, REPOSITORY, SYNCHRONIZED, INSERTTIME) VALUES (\"{0}\", \"{1}\", \"{2}\", \"{3}\", \"{4}\")", e.Item.Id, e.EventType.ToString(), e.RepositoryType.ToString(), bool.FalseString, DateTime.Now.ToString());
                ExecuteNonQuery (sql);
            }
        }

        public override List<Event> All
        {
            get{
                return ExecuteReader ("SELECT * FROM EVENT");
            }
        }

        public override void UpdateToSynchronized (Event e)
        {            
            string id = new SQLiteRepositoryItemDAO().GetId(e.Item);
            ExecuteNonQuery (string.Format("UPDATE EVENT SET  SYNCHRONIZED = \"{0}\" WHERE ITEMID =\"{1}\"", bool.TrueString, id));
        }

 
        public override List<Event> EventsNotSynchronized {
            get {
                return ExecuteReader (string.Format("SELECT * FROM EVENT WHERE SYNCHRONIZED =\"{0}\"", bool.FalseString));
            }
        }


        #endregion

        public bool NotExistsConflict (Event e)
        {

            string sql = string.Format("SELECT * FROM EVENT WHERE REPOSITORY <> \"{0}\" AND (SYNCHRONIZED = \"{1}\" OR INSERTTIME > \"{2}\" ) AND ITEMID = {3}", e.RepositoryType.ToString(), bool.FalseString, DateTime.Now.Subtract(new TimeSpan(0,0,20)),e.Item.Id);

            return ExecuteReader (sql).Count == 0;
        }
        
        public bool Exists (Event e)
        {
            return All.Count!=0;
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

        public List<Event> ExecuteReader(string sqlCommand) {
            List<Event> events = new List<Event>();
            using (var conn= new SqliteConnection(SQLiteDatabase.ConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sqlCommand;
                    cmd.CommandType = System.Data.CommandType.Text;
                    SqliteDataReader reader = cmd.ExecuteReader(); 
                    while (reader.Read()){
                        Event e = new Event();
                        e.Item = repositoryItemDAO.GetById (reader.GetInt32(1));
                        e.EventType = (EventType) Enum.Parse(typeof(EventType), reader.GetString(2));
                        e.RepositoryType = (RepositoryType) Enum.Parse(typeof(RepositoryType),reader.GetString(3));
                        e.Synchronized = bool.Parse (reader.GetString(4));
                        e.InsertTime = Convert.ToDateTime (reader.GetString (5));
                        

                        events.Add (e);
                    }                    
                }
            }
            return events;
        }  


	}

}

