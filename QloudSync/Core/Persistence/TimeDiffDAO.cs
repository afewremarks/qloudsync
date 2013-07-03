using System;
using System.IO;
using System.Collections.Generic;
using GreenQloud.Model;
using System.Linq;

namespace GreenQloud.Persistence
{
    public abstract class TimeDiffDAO
    {
        public abstract void Create (double diff);
        public abstract double Count {
            get;
        }
        public abstract double Last{
            get;
        }
    }
}

