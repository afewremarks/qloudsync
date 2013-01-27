using System;
using System.Collections.Generic;
using QloudSync.Repository;


namespace  QloudSync.Synchrony
{
    public class Sync
    {

        public Sync ()
        {
        }
        public DateTime Time{ set; get; }
        public List<Change> Changes{ set; get; }
    }

}

