using GreenQloud.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QloudSyncCore.Core.Persistence
{
    public class TimeDiffRaven
    {
        public void Create (double e)
        {
            TimeDiff timeDiff = new TimeDiff();
            timeDiff.Diff = e ;
            DataDocumentStore.Insert(timeDiff);
        }
        public double Last {
            get {
                return DataDocumentStore.Instance.OpenSession().Query<TimeDiff>().OrderByDescending(t => t.Id).First().Diff ;
            }
        }
        public double Count {
            get {
                return DataDocumentStore.Count<TimeDiff>();
            }
        }
    }
}
