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
using System.Data;

namespace GreenQloud.Persistence
{
	public class SQLiteEventDAO : EventDAO
	{

        SQLiteRepositoryItemDAO repositoryItemDAO = new SQLiteRepositoryItemDAO();
        #region implemented abstract members of EventDAO
        SQLiteDatabase database = new SQLiteDatabase();
        public override void Create (Event e)
        {
            if (e == null)
                return;
            e.Item = repositoryItemDAO.Create (e);
            bool noConflicts = NotExistsConflict(e);

           if (noConflicts){
                string sql =string.Format("INSERT INTO EVENT (ITEMID, TYPE, REPOSITORY, SYNCHRONIZED, INSERTTIME) VALUES (\"{0}\", \"{1}\", \"{2}\", \"{3}\", \"{4}\")", e.Item.Id, e.EventType.ToString(), e.RepositoryType.ToString(), bool.FalseString, DateTime.Now.ToString());
                Console.WriteLine (sql);

                database.ExecuteNonQuery (sql);
            }
        }

        public override List<Event> All
        {
            get{
                return Select ("SELECT * FROM EVENT");
            }
        }

        public override void UpdateToSynchronized (Event e)
        {            
            string id = repositoryItemDAO.GetId(e.Item);
            database.ExecuteNonQuery (string.Format("UPDATE EVENT SET  SYNCHRONIZED = \"{0}\" WHERE ITEMID =\"{1}\"", bool.TrueString, id));
        }

 
        public override List<Event> EventsNotSynchronized {
            get {
                List<Event> list = Select (string.Format("SELECT * FROM EVENT WHERE SYNCHRONIZED =\"{0}\"", bool.FalseString));

                return list;
            }
        }

        public override void CreateWithoutType (Event e){
           
            e.Item = repositoryItemDAO.Create (e);
            string sql = string.Format("INSERT INTO EVENT (ITEMID, TYPE, REPOSITORY, SYNCHRONIZED, INSERTTIME) VALUES (\"{0}\", \"NULL\", \"{1}\", \"{2}\", \"{3}\")", e.Item.Id, e.RepositoryType.ToString(), bool.FalseString, DateTime.Now.ToString());
            //Console.WriteLine (sql);
            database.ExecuteNonQuery (sql);
        }

        public override void SetEventType (Event e)
        {
            string itemId = repositoryItemDAO.GetId (e.Item);
            database.ExecuteNonQuery (string.Format("UPDATE EVENT SET  TYPE = \"{0}\" WHERE ITEMID =\"{1}\"", e.EventType, itemId));
        }

        public override void RemoveAllUnsynchronized ()
        {
            List<Event> list = EventsNotSynchronized;
            foreach (Event e in list)
            {
                UpdateToSynchronized (e);
            }
        }

        #endregion



        public bool NotExistsConflict (Event e)
        {
           
            string sql = string.Format("SELECT * FROM EVENT WHERE REPOSITORY <> \"{0}\" AND SYNCHRONIZED = \"{1}\" AND ITEMID = {2}", e.RepositoryType.ToString(), bool.FalseString,e.Item.Id);
           
            List<Event> list = Select (sql);
            if (list.Count == 0)
            {

                list = Select (string.Format("SELECT * FROM EVENT WHERE ITEMID = \"{0}\" AND REPOSITORY <> \"{1}\" ", e.Item.Id, e.RepositoryType));
                if (list.Any (ev=> ev.InsertTime >  DateTime.Now.Subtract(new TimeSpan(0,0,60)))){
                    return false;
                }
            }
            else{
                if (e.RepositoryType == RepositoryType.REMOTE){
                    if (list.Any (ev=> ev.EventType == EventType.NULL))
                        return false;
                }
            }
            return true;
        }
        
        public bool Exists (Event e)
        {
            return All.Count!=0;
        }

        public List<Event> Select (string sql){
















            List<Event> events = new List<Event>();
            DataTable dt = database.GetDataTable(sql);
            foreach(DataRow dr in dt.Rows){
                Event e = new Event();
                e.Item = repositoryItemDAO.GetById (int.Parse (dr[1].ToString()));

                e.EventType = (EventType) Enum.Parse(typeof(EventType), dr[2].ToString());
                e.RepositoryType = (RepositoryType) Enum.Parse(typeof(RepositoryType),dr[3].ToString());
                e.Synchronized = bool.Parse (dr[4].ToString());
                e.InsertTime = Convert.ToDateTime (dr[5].ToString());
                
                events.Add (e);
            }
            return events;
        }
  	}   
}

