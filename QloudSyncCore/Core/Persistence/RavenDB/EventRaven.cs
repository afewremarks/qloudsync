using GreenQloud;
using GreenQloud.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QloudSyncCore.Core.Persistence
{
    public class EventRaven
    {

        RepositoryItemRaven repositoryItemRaven = new RepositoryItemRaven();
        LocalRepository repo;

        public EventRaven(LocalRepository repo){
            this.repo = repo;
        }

        public EventRaven(){
        }

        public void Create (Event e)
        {
            if (e == null)
                return;

           try{
                repositoryItemRaven.Create (e);

                DateTime dateOfEvent = e.InsertTime;
                if (dateOfEvent.Equals(DateTime.MinValue))
                {
                    dateOfEvent = GlobalDateTime.Now;
                }
                e.InsertTime = dateOfEvent;


                //Verify ignore
                RepositoryIgnoreRaven ignoreDato = new RepositoryIgnoreRaven();
                List<RepositoryIgnore> ignores = ignoreDato.All(this.repo);
                bool ignore = false;
                foreach (RepositoryIgnore ignoreItem in ignores)
                {
                    if (e.Item.Key.StartsWith(ignoreItem.Path))
                    {
                        e.Synchronized = true;
                        e.Response = RESPONSE.IGNORED;
                    }
                }

                
                DataDocumentStore.Insert(e);

                Logger.LogEvent("EVENT CREATED", e);
                if (e.Response == RESPONSE.IGNORED)
                {
                    Logger.LogEvent("EVENT MARKED TO IGNORE", e);
                }
            }catch(Exception err){
                Logger.LogInfo("ERROR", err);
            }
        }

        public List<Event> All
        {
            get{
                return DataDocumentStore.GetAll<Event>();
            }
        }
        public List<Event> LastEvents{
            get{
                List<Event> events = DataDocumentStore.Instance.OpenSession().Query<Event>().Where(
                    ev => ev.Synchronized == true && ev.Response == RESPONSE.OK && !ev.Item.IsFolder &&
                          (!ev.Item.Moved || !ev.Item.ResultItem.Moved)
                    ).ToList();
                return events;
            }
        }
        public Event FindById(int id)
        {
            return DataDocumentStore.Instance.OpenSession().Load<Event>(id);
        }

        public void UpdateToSynchronized (Event e, RESPONSE response)
        {
           var session = DataDocumentStore.Instance.OpenSession();
           Event ev = session.Load<Event>(e.Id);
           ev.Synchronized = true;
           ev.Response = response;
           session.SaveChanges();
        }

        public void IgnoreAllEquals (Event e)
        {
            var session = DataDocumentStore.Instance.OpenSession();
            List<Event> events = session.Query<Event>().Where
                (ev => ev.ItemId == e.Item.Id && ev.EventType == e.EventType && ev.Id > e.Id && ev.Synchronized != true && ev.RepositoryId == repo.Id).ToList();
            foreach (Event ev in events)
            {
                ev.Synchronized = true;
                ev.Response = RESPONSE.IGNORED;
            }
            session.SaveChanges();
        }

        public void IgnoreAllIfDeleted (Event e)
        {
            var session = DataDocumentStore.Instance.OpenSession();
            List<Event> list = session.Query<Event>().Where
                (ev => ev.ItemId == e.Item.Id && ev.EventType == EventType.DELETE && ev.Id > e.Id && ev.Synchronized != true && ev.RepositoryId == repo.Id).ToList();
            if(list.Count > 0) { 
                List<Event> events = session.Query<Event>().Where
                    (ev => ev.ItemId == e.Item.Id && ev.Id < list.Last().Id && ev.RepositoryId == repo.Id).ToList();
                foreach (Event ev in events)
                {
                    ev.Synchronized = true;
                    ev.Response = RESPONSE.IGNORED;
                }
                session.SaveChanges();
                repositoryItemRaven.MarkAsMoved (e.Item);
                if (e.EventType == EventType.CREATE) {
                    Event ev = session.Load<Event>(list.Last().Id);
                    ev.Synchronized = true;
                    ev.Response = RESPONSE.IGNORED;
                    session.SaveChanges();
                }
            }
        }

        public void IgnoreFromIgnordList(Event e)
        {
            RepositoryIgnoreRaven ignoreDato = new RepositoryIgnoreRaven();
            List<RepositoryIgnore> ignores = ignoreDato.All(this.repo);
            bool ignore = false;
            foreach (RepositoryIgnore ignoreItem in ignores)
            {
                if (e.Item.Key.StartsWith(ignoreItem.Path) || (e.EventType == EventType.MOVE && e.Item.ResultItem.Key.StartsWith(ignoreItem.Path)))
                {
                    ignore = true;
                }
            }
            if (ignore)
                UpdateToSynchronized(e, RESPONSE.IGNORED);
        }

        public void CombineMultipleMoves (Event e){
            Event toCombine = null;
            Event combineWith = null;
            var session = DataDocumentStore.Instance.OpenSession();
            if (e.EventType == EventType.MOVE)
                toCombine = e;

            if(e == null) { 
                List<Event> list = session.Query<Event>().Where
                    (ev => ev.ItemId == e.Item.Id && ev.EventType == EventType.MOVE && ev.Id > e.Id && ev.Synchronized != true && ev.RepositoryId == repo.Id).ToList();
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
                        list2 = session.Query<Event>().Where
                            (ev => ev.ItemId == toCombine.Item.ResultItemId && ev.EventType == EventType.MOVE && ev.Id > e.Id && ev.Synchronized != true && ev.RepositoryId == repo.Id).ToList(); 
                        if (list2.Count > 0) { 
                            combineWith = list2.First ();
                            Event firstEvent = session.Load<Event>(combineWith.Id);
                            firstEvent.Synchronized = true;
                            firstEvent.Response = RESPONSE.IGNORED;
                            repositoryItemRaven.MarkAsMoved (toCombine.Item.ResultItem);
                            toCombine.Item.ResultItem = combineWith.Item.ResultItem;
                            List<RepositoryItem> repos = session.Query<RepositoryItem>().Where
                                (r => r.Id.Equals(toCombine.Item.Id) && r.Id == repo.Id).ToList();
                            foreach (RepositoryItem rps in repos)
                            {
                                rps.ResultItemId = combineWith.Item.Id;
                            }
                            session.SaveChanges();
                        }
                    } while (list2 != null && list2.Count > 0);
                }
            } catch (Exception ex) {
                Logger.LogInfo("ERROR", ex.Message);
            }
        }

        public void IgnoreAllIfMoved (Event e){
            CombineMultipleMoves (e);
            e = FindById(e.Id);
            var session = DataDocumentStore.Instance.OpenSession();
            List<Event> list = session.Query<Event>().Where
                (ev => ev.ItemId == e.Item.Id && ev.EventType == EventType.MOVE && ev.Id > e.Id && ev.Synchronized == true && ev.RepositoryId == repo.Id).ToList();
            if(list.Count > 0) { 
                if (e.EventType == EventType.CREATE || e.EventType == EventType.UPDATE) {
                    Event firstEvent = session.Load<Event>(list.First().Id);
                    firstEvent.Synchronized = true;
                    firstEvent.Response = RESPONSE.IGNORED;
                    repositoryItemRaven.MarkAsMoved(e.Item);
                    Event secondEvent = session.Load<Event>(e.Id);
                    secondEvent.Item.Id = list.First().Item.ResultItem.Id;
                    session.SaveChanges();
                }
            }
        }

        public void UpdateTryQnt (Event e)
        {
            var session = DataDocumentStore.Instance.OpenSession();
            Event ev = session.Load<Event>(e.Id);
            ev.TryQnt = e.TryQnt;
            session.SaveChanges();
        }

        public List<Event> EventsNotSynchronized {
            get {
                List<Event> list = DataDocumentStore.Instance.OpenSession().Query<Event>().Where
                    (ev => ev.Synchronized == false && ev.RepositoryId == repo.Id).ToList();
                return list;
            }
        }

        public void SetEventType (Event e)
        {
            var session = DataDocumentStore.Instance.OpenSession();
            Event ev = session.Load<Event>(e.Id);
            ev.EventType = e.EventType;
            session.SaveChanges();
        }

        public void RemoveAllUnsynchronized ()
        {
            List<Event> list = EventsNotSynchronized;
            foreach (Event e in list)
            {
                UpdateToSynchronized (e, RESPONSE.IGNORED);
            }
        }

        
        public DateTime LastSyncTime{
            get{
                var session =  DataDocumentStore.Instance.OpenSession();
                Event eventLimit1;
                var query = session.Query<Event>().Where
                    (ev => ev.RepositoryId == repo.Id && ev.RepositoryType == RepositoryType.REMOTE).OrderByDescending(ev => ev.InsertTime);
                if (query.Count() > 0) {
                        eventLimit1 = query.First();
                } else {
                    eventLimit1 = session.Query<Event>().Where
                    (ev => ev.RepositoryId == repo.Id && ev.RepositoryType == RepositoryType.LOCAL).OrderByDescending(ev => ev.InsertTime).First();
                }

                string time = null;
                if (eventLimit1 != null)
                    time = eventLimit1.InsertTime.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
                if(time == null)
                    return DateTime.MinValue;
                try{

                    DateTime dtime =  Convert.ToDateTime(time);// DateTime.ParseExact(time, "dd/MM/yyyy hh:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                    return dtime.AddSeconds(1).ToUniversalTime();
                }catch(Exception e )
                {
                    Logger.LogInfo("ERROR", e.Message);
                }
                return DateTime.MaxValue.ToUniversalTime();
            }
        }
        
        public bool Exists (Event e)
        {
            return DataDocumentStore.GetAll<Event>().Count() != 0;
        }
    }
}
