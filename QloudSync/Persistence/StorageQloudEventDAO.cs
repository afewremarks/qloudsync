using System;
using GreenQloud.Repository.Remote;
using GreenQloud.Repository;
using GreenQloud.Persistence;
using GreenQloud.Repository.Local;
using GreenQloud.Model;

namespace GreenQloud.Persistence
{
	public class StorageQloudEventDAO : EventDAO
	{
        #region implemented abstract members of EventDAO

        public override void Create (Event e)
        {
            throw new NotImplementedException ();
        }

        public override System.Collections.Generic.List<Event> All
        {
            get{
                throw new NotImplementedException ();
            }
        }

        public override System.Collections.Generic.List<Event> EventsNotSynchronized
        {
            get{
                throw new NotImplementedException ();
            }
        }

        public override void UpdateToSynchronized (Event e)
        {
            throw new NotImplementedException ();
        }

        #endregion


	}

}

