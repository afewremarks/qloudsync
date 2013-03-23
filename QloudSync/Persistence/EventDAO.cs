using System;
using GreenQloud.Model;
using System.Collections.Generic;

namespace GreenQloud.Persistence
{
    public abstract class EventDAO
    {
        public abstract void Create (Event e);
        public abstract List<Event> GetEventsNotSynchronized();
        public abstract void UpdateToSynchronized (Event e);
    }
}

