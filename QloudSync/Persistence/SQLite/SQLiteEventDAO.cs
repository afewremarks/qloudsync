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

namespace GreenQloud.Persistence.SQLite
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
            bool noConflicts = !ExistsConflict(e);

           if (noConflicts){
                try{
                    string dateOfEvent =  e.InsertTime;
                    if(dateOfEvent==null){
                        dateOfEvent = GlobalDateTime.NowUniversalString;
                    }

                    string sql =string.Format("INSERT INTO EVENT (ITEMID, TYPE, REPOSITORY, SYNCHRONIZED, INSERTTIME, USER, APPLICATION, APPLICATION_VERSION, DEVICE_ID, OS, BUCKET) VALUES (\"{0}\", \"{1}\", \"{2}\", \"{3}\", \"{4}\", \"{5}\", \"{6}\", \"{7}\", \"{8}\", \"{9}\", \"{10}\")", 
                                              e.Item.Id, e.EventType.ToString(), e.RepositoryType.ToString(), bool.FalseString, dateOfEvent, e.User, e.Application, e.ApplicationVersion, e.DeviceId, e.OS, e.Bucket);

                    database.ExecuteNonQuery (sql);
                }catch(Exception err){
                    Logger.LogInfo("ERROR", err);
                }
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

        
        public override string LastSyncTime{
            get{
                List<Event> events = Select("SELECT * FROM EVENT WHERE REPOSITORY = \"REMOTE\" ORDER BY INSERTTIME DESC LIMIT 1");
                if(events.Count == 0)
                    return string.Empty;

                string time = events[0].InsertTime;
                if(time == null)
                    return string.Empty;
                try{

                    DateTime dtime =  Convert.ToDateTime(time);// DateTime.ParseExact(time, "dd/MM/yyyy hh:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                    return dtime.AddSeconds(1).ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
                }catch(Exception e )
                {
                    Logger.LogInfo("ERROR", e.Message);
                }
                return DateTime.MaxValue.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");;
            }
        }

        #endregion

        public bool ExistsConflict (Event e)
        {
            string query = "SELECT * FROM EVENT WHERE REPOSITORY= \"{0}\" AND ITEMID = \"{1}\"";
            string sql ="";
            DateTime limitDate = GlobalDateTime.Now.Subtract(new TimeSpan(0,0,60));
            if (e.RepositoryType == RepositoryType.LOCAL)
            {
                sql = string.Format (query, RepositoryType.REMOTE, e.Item.Id);
            }
            else{
                sql = string.Format (query, RepositoryType.LOCAL, e.Item.Id);
            }

            List<Event> list = Select (sql);

            foreach (Event ev in list)
            {
               if (!ev.Synchronized)
                    return true;
                if (e.InsertTime != null){
                    try{

                        if(Convert.ToDateTime(e.InsertTime)  >limitDate){
                            return true;
                        }
                    }catch{}
                }
            }

            return false;
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
                e.InsertTime = dr[5].ToString();
                e.User = dr[6].ToString();
                e.Application = dr[7].ToString();
                e.ApplicationVersion = dr[8].ToString();
                e.DeviceId = dr[9].ToString();
                e.OS = dr[10].ToString();
                e.Bucket = dr[11].ToString();
                events.Add (e);
            }
            return events;
        }
  	}   
}

