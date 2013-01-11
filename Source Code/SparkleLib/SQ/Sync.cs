using System;
using System.Collections.Generic;
using SQ.Repository;

namespace  SQ.Synchrony
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

