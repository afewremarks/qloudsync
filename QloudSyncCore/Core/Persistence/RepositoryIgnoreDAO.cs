using System;
using System.IO;
using System.Collections.Generic;
using GreenQloud.Model;
using System.Linq;

namespace GreenQloud.Persistence
{
    public abstract class RepositoryIgnoreDAO
    {
        public abstract void Create (LocalRepository repo,  string path);
        public abstract List<RepositoryIgnore> All(LocalRepository repo);
    }
}

