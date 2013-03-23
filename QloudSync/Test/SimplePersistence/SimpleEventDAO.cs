using System;
using NUnit.Framework;
using GreenQloud.Synchrony;
using System.IO;
using GreenQloud.Test.SimpleRepository;
using GreenQloud.Persistence;
using GreenQloud.Repository.Local;
using GreenQloud.Model;
using System.Collections.Generic;
using System.Linq;

namespace GreenQloud.Test.SimplePersistence
{
	public class SimpleEventDAO : EventDAO
	{
        public List<Event> list = new List<Event>();
        #region implemented abstract members of EventDAO

        public override void Create (Event e)
        { 
            list.Add (e);
        }

        public override System.Collections.Generic.List<Event> EventsNotSynchronized 
        {
            get{
                return list.Where (e=>e.Synchronized == false).ToList<Event>();
            }
        }

        public override void UpdateToSynchronized (Event e)
        {
            int id = list.IndexOf (e);
            e.Synchronized = true;
            list[id] = e;
        }

        public override List<Event> All{
            get{
                return list;
            }
        }

        #endregion


	}

}

