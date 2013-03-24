using System;
using System.IO;
using System.Collections.Generic;
using GreenQloud.Model;
using System.Linq;

namespace GreenQloud.Persistence
{
    public abstract class RepositoryDAO
    {
        public abstract void Create (LocalRepository e);
        public abstract List<LocalRepository> All{
            get;
        }
    }
}

