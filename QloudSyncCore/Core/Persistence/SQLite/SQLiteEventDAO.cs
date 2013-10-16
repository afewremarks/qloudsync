using System;
using GreenQloud.Repository;
using GreenQloud.Persistence;
using GreenQloud.Repository;
using GreenQloud.Model;
using System.Collections.Generic;
using GreenQloud.Persistence.SQLite;
using System.Linq;
using System.Data;

namespace GreenQloud.Persistence.SQLite
{
	public class SQLiteEventDAO : EventDAO
	{

        SQLiteRepositoryItemDAO repositoryItemDAO = new SQLiteRepositoryItemDAO();
        SQLiteDatabase database = SQLiteDatabase.Instance ();
        LocalRepository repo;

        public SQLiteEventDAO(LocalRepository repo){
            this.repo = repo;
        }
        public SQLiteEventDAO(){
        }

        public void CreateIfNotExistsAny(Event e) {
            if (!ExistsAny(e)) {
                Create(e);
            }            
        }

        public override void Create (Event e)
        {
            if (e == null)
                return;
            bool noConflicts = !ExistsConflict(e);

           if (noConflicts){
                try{
                    repositoryItemDAO.Create (e);

                    DateTime dateOfEvent =  e.InsertTime;
                    if(dateOfEvent==DateTime.MinValue){
                        dateOfEvent = GlobalDateTime.Now;
                    }

                    //Verify ignore
                    RepositoryIgnoreDAO ignoreDato = new SQLiteRepositoryIgnoreDAO();
                    List<RepositoryIgnore> ignores = ignoreDato.All(this.repo);
                    foreach (RepositoryIgnore ignoreItem in ignores)
                    {
                        if (e.Item.Key.StartsWith(ignoreItem.Path))
                        {
                            e.Synchronized = true;
                            e.Response = RESPONSE.IGNORED;
                        }
                    }
                    


                    string sql =string.Format("INSERT INTO EVENT (ITEMID, TYPE, REPOSITORY, SYNCHRONIZED, INSERTTIME, USER, APPLICATION, APPLICATION_VERSION, DEVICE_ID, OS, BUCKET, TRY_QNT, RESPONSE, RepositoryId) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}', '{10}', '{11}', '{12}', '{13}')", 
                                              e.Item.Id, e.EventType.ToString(), e.RepositoryType.ToString(), e.Synchronized.ToString(), dateOfEvent, e.User, e.Application, e.ApplicationVersion, e.DeviceId, e.OS, e.Bucket, e.TryQnt, e.Response.ToString(), e.Repository.Id);

                    e.Id = (int) database.ExecuteNonQuery (sql, true);

                    Logger.LogEvent("EVENT CREATED", e);
                    if (e.Response == RESPONSE.IGNORED) {
                        Logger.LogEvent("EVENT MARKED TO IGNORE", e);
                    }

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
        public override List<Event> LastEvents{
            get{
                string sql = string.Format("SELECT TOP '{6}' e.* FROM Event e INNER JOIN  RepositoryItem ie ON ie.RepositoryItemId = e.ItemId " +
                "WHERE (" +
                    "ie.RepositoryItemId IN (SELECT r1.RepositoryItemId FROM RepositoryItem r1 WHERE r1.MOVED <> '{0}')" +
                    "OR ie.RepositoryItemId IN" +
                    "(SELECT r2.RepositoryItemId FROM RepositoryItem r2 INNER JOIN RepositoryItem r3 ON r2.ResultItemId = r3.RepositoryItemId WHERE r2.MOVED = '{1}' AND r3.MOVED <> '{2}')" +
                    ") AND e.SYNCHRONIZED = '{3}' AND ie.isfolder <> '{4}' AND e.RESPONSE = '{5}' " +
                 "GROUP BY ItemId ORDER BY EventID DESC",bool.TrueString,bool.TrueString,bool.TrueString,bool.TrueString,bool.TrueString, RESPONSE.OK, 5);

                return Select (sql);
            }
        }
        public override Event FindById(int id)
        {
            return Select (string.Format("SELECT * FROM EVENT WHERE EventID = '{0}'", id)).FirstOrDefault();
        }

        public override void UpdateToSynchronized (Event e, RESPONSE response)
        {            
            database.ExecuteNonQuery (string.Format("UPDATE EVENT SET  SYNCHRONIZED = '{1}', RESPONSE = '{2}' WHERE EventID ='{0}'", e.Id, bool.TrueString, response.ToString()));
        }

        public override void IgnoreAllEquals (Event e)
        {            
            database.ExecuteNonQuery (string.Format("UPDATE EVENT SET  SYNCHRONIZED = '{1}', RESPONSE = '{4}' WHERE ItemId ='{0}' AND TYPE = '{2}' AND EventID > '{3}' AND SYNCHRONIZED <> '{5}' AND RepositoryId = '{6}'", e.Item.Id , bool.TrueString , e.EventType, e.Id, RESPONSE.IGNORED.ToString(), bool.TrueString, repo.Id));
        }

        public override void IgnoreAllIfDeleted (Event e)
        {            
            List<Event> list = Select (string.Format("SELECT * FROM EVENT WHERE ItemId ='{0}' AND TYPE = '{1}' AND EventID > '{2}'  AND SYNCHRONIZED <> '{3}' AND RepositoryId = '{4}'", e.Item.Id, EventType.DELETE, e.Id, bool.TrueString, repo.Id));
            if(list.Count > 0) { 
                database.ExecuteNonQuery (string.Format("UPDATE EVENT SET  SYNCHRONIZED = '{0}', RESPONSE = '{1}' WHERE ItemId ='{2}' AND EventID < '{3}'  AND RepositoryId = '{4}' ", bool.TrueString, RESPONSE.IGNORED.ToString(), e.Item.Id , list.Last().Id, repo.Id));
                repositoryItemDAO.MarkAsMoved (e.Item);
                if (e.EventType == EventType.CREATE) {
                    database.ExecuteNonQuery (string.Format("UPDATE EVENT SET  SYNCHRONIZED = '{0}', RESPONSE = '{1}' WHERE EventID = '{2}'", bool.TrueString, RESPONSE.IGNORED.ToString(), list.Last().Id));
                }
            }
        }

        public override void IgnoreFromIgnordList(Event e)
        {
            RepositoryIgnoreDAO ignoreDato = new SQLiteRepositoryIgnoreDAO();
            List<RepositoryIgnore> ignores = ignoreDato.All(this.repo);
            bool ignore = false;
            foreach (RepositoryIgnore ignoreItem in ignores)
            {
                if (e.Item.Key.StartsWith(ignoreItem.Path) || (e.EventType == EventType.MOVE && e.Item.ResultItem.Key.StartsWith(ignoreItem.Path)))
                {
                    ignore = true;
                }
            }
            if(ignore)
                database.ExecuteNonQuery(string.Format("UPDATE EVENT SET  SYNCHRONIZED = '{0}', RESPONSE = '{1}' WHERE EventID = '{2}'", bool.TrueString, RESPONSE.IGNORED.ToString(), e.Id));
        }

        public void CombineMultipleMoves (Event e){
            Event toCombine = null;
            Event combineWith = null;
            if (e.EventType == EventType.MOVE)
                toCombine = e;

            if(e == null) { 
                List<Event> list = Select (string.Format("SELECT * FROM EVENT WHERE ItemId ='{0}' AND TYPE = '{1}' AND EventID > '{2}'  AND SYNCHRONIZED <> '{3}'  AND RepositoryId = '{4}'", e.Item.Id, EventType.MOVE, e.Id, bool.TrueString, repo.Id));
                if(list.Count > 0) { 
                    toCombine = list.First ();   
                }
            }

            //do while move.hasnext
            //ignore o next
            try {
                if (toCombine != null){
                    List<Event> list2;
                    do {
                        list2 = Select (string.Format("SELECT * FROM EVENT WHERE ItemId ='{0}' AND TYPE = '{1}' AND EventID > '{2}'  AND SYNCHRONIZED <> '{3}' AND RepositoryId = '{4}'", toCombine.Item.ResultItemId, EventType.MOVE, e.Id, bool.TrueString, repo.Id));
                        if (list2.Count > 0) { 
                            combineWith = list2.First ();  
                            database.ExecuteNonQuery (string.Format("UPDATE EVENT SET  SYNCHRONIZED = '{0}', RESPONSE = '{1}' WHERE EventID = '{2}'", bool.TrueString, RESPONSE.IGNORED.ToString(), combineWith.Id));
                            repositoryItemDAO.MarkAsMoved (toCombine.Item.ResultItem);
                            toCombine.Item.ResultItem = combineWith.Item.ResultItem;
                            database.ExecuteNonQuery (string.Format("UPDATE RepositoryItem SET  ResultItemId ='{0}' WHERE RepositoryItemID = '{1}'  AND RepositoryId = '{2}'", combineWith.Item.ResultItemId, toCombine.Item.Id, repo.Id));
                        }
                    } while (list2 != null && list2.Count > 0);
                }
            } catch (Exception ex) {
                Logger.LogInfo("ERROR", ex.Message);
            }
        }

        public override void IgnoreAllIfMoved (Event e){
            CombineMultipleMoves (e);
            e = FindById(e.Id);
            List<Event> list = Select (string.Format("SELECT * FROM EVENT WHERE ItemId ='{0}' AND TYPE = '{1}' AND EventID > '{2}'  AND SYNCHRONIZED <> '{3}' AND RepositoryId = '{4}'", e.Item.Id, EventType.MOVE, e.Id, bool.TrueString, repo.Id));
            if(list.Count > 0) { 
                if (e.EventType == EventType.CREATE || e.EventType == EventType.UPDATE) {
                    database.ExecuteNonQuery (string.Format("UPDATE EVENT SET  SYNCHRONIZED = '{0}', RESPONSE = '{1}' WHERE EventID = '{2}'", bool.TrueString, RESPONSE.IGNORED.ToString(), list.First().Id));
                    repositoryItemDAO.MarkAsMoved (e.Item);
                    database.ExecuteNonQuery (string.Format("UPDATE EVENT SET  ItemId ='{0}' WHERE EventID = '{1}'", list.First().Item.ResultItem.Id, e.Id));
                }
            }
        }

        public override void UpdateTryQnt (Event e)
        {
            database.ExecuteNonQuery (string.Format("UPDATE EVENT SET  TRY_QNT = '{0}' WHERE EventID ='{1}'", e.TryQnt, e.Id));
        }

        public override List<Event> EventsNotSynchronized {
            get {
                string sql = string.Format ("SELECT * FROM EVENT WHERE SYNCHRONIZED ='{0}' AND INSERTTIME < '{1}' AND RepositoryId = '{2}' ORDER BY EventID ASC", bool.FalseString, GlobalDateTime.Now.AddSeconds (-10).ToString ("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"), repo.Id);
                List<Event> list = Select (sql);
                return list;
            }
        }

        public override void SetEventType (Event e)
        {
            database.ExecuteNonQuery (string.Format("UPDATE EVENT SET  TYPE = '{0}' WHERE EventID ='{1}'", e.EventType, e.Id));
        }

        public override void RemoveAllUnsynchronized ()
        {
            List<Event> list = EventsNotSynchronized;
            foreach (Event e in list)
            {
                UpdateToSynchronized (e, RESPONSE.IGNORED);
            }
        }

        
        public override DateTime LastSyncTime{
            get{
                List<Event> events = Select(string.Format("SELECT TOP 1 * FROM EVENT WHERE REPOSITORY = 'REMOTE' AND RepositoryId = '{0}' ORDER BY INSERTTIME DESC", repo.Id));
                if (events.Count == 0)
                    events = Select(string.Format("SELECT TOP 1 * FROM EVENT WHERE REPOSITORY = 'LOCAL' AND RepositoryId = '{0}' ORDER BY INSERTTIME DESC", repo.Id));

                if (events.Count > 0)
                {
                    return  events[0].InsertTime;
                }
                else
                {
                    return DateTime.MinValue.ToUniversalTime();
                }
            }
        }

        public bool ExistsAny(Event e)
        {
            System.Object temp = database.ExecuteScalar(
                string.Format("SELECT count(*) FROM EVENT WHERE SYNCHRONIZED <> '{0}' AND (ItemId='{1}' OR ItemId='{2}')  AND RepositoryId = '{3}'", bool.TrueString, e.Item.Id, (e.HaveResultItem ? e.Item.ResultItemId : 0), repo.Id)
            );
            int count = int.Parse(temp.ToString());

            if (count > 0)
            {
                return true;
            }

            return false;
        }

        public bool ExistsConflict (Event e)
        {
            System.Object temp = database.ExecuteScalar (
                string.Format ("SELECT count(*) FROM EVENT WHERE ItemId ='{0}' AND TYPE = '{1}' AND SYNCHRONIZED <> '{2}'  AND RepositoryId = '{3}'", e.Item.Id, e.EventType.ToString(), bool.TrueString, repo.Id)
                        );
            int count = int.Parse (temp.ToString());

            if (count > 0) {
                return true;
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
                Event e = new Event(LocalRepository.CreateInstance(int.Parse(dr[14].ToString())));
                e.Id = int.Parse (dr[0].ToString());
                e.Item = repositoryItemDAO.GetById (int.Parse (dr[1].ToString()));
                e.EventType = (EventType) Enum.Parse(typeof(EventType), dr[2].ToString());
                e.RepositoryType = (RepositoryType) Enum.Parse(typeof(RepositoryType),dr[3].ToString());
                e.Synchronized = bool.Parse (dr[4].ToString());
                e.InsertTime = Convert.ToDateTime(dr[5].ToString());
                e.User = dr[6].ToString();
                e.Application = dr[7].ToString();
                e.ApplicationVersion = dr[8].ToString();
                e.DeviceId = dr[9].ToString();
                e.OS = dr[10].ToString();
                e.Bucket = dr[11].ToString();
                e.TryQnt = int.Parse (dr[12].ToString());
                e.Response = (RESPONSE) Enum.Parse(typeof(RESPONSE),dr[13].ToString());
                events.Add (e);
            }
            return events;
        }
  	}   
}

