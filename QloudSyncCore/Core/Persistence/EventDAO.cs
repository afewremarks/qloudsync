using System;
using GreenQloud.Model;
using System.Collections.Generic;

namespace GreenQloud.Persistence
{
    public abstract class EventDAO
    {
        public abstract void Create (Event e);
        public abstract List<Event> All{
            get;
        }
        public abstract List<Event> EventsNotSynchronized{
            get;
        }
        public abstract List<Event> LastEvents{
            get;
        }

        public abstract Event FindById (int id);
        public abstract void UpdateToSynchronized (Event e, RESPONSE response);
        public abstract void UpdateTryQnt (Event e);
        public abstract void IgnoreAllEquals (Event e);
        public abstract void IgnoreAllIfDeleted (Event e);
        public abstract void IgnoreAllIfMoved (Event e);
        public abstract void SetEventType (Event e);
        public abstract void RemoveAllUnsynchronized ();
        public abstract string LastSyncTime{get;}
        public abstract void IgnoreFromIgnordList(Event e);
    }
}

