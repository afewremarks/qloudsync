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
        public abstract void UpdateToSynchronized (Event e);
        public abstract void UpdateResultObject (Event e);

        public abstract void SetEventType (Event e);

        public abstract void RemoveAllUnsynchronized ();
        public abstract string LastSyncTime{get;}
    }
}

