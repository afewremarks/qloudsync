using System;
using System.Collections.Generic;
using GreenQloud.Repository;


namespace GreenQloud.Synchrony
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

