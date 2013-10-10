using QloudSyncCore.Core.Persistence;
using System;
using System.Collections.Generic;

namespace GreenQloud.Model
{
    public class LocalRepository
    {
        private static readonly RepositoryRaven dao =  new RepositoryRaven ();

        public LocalRepository(string path, string remoteFolder, bool active = true, bool recovering = true){
            this.Path = path;
            this.RemoteFolder = remoteFolder;
            this.Active = active;
            this.Recovering = true;
        }

        public static LocalRepository CreateInstance (int id)
        {
            return dao.GetById (id);
        }

        public int Id {
            get;
            set;
        }

        public string Path {
            get;
            set;
        }
        public string RemoteFolder {
            get;
            set;
        }

        public bool Recovering {
            get;
            set;
        }

        public class LocalRepositoryComparer : IEqualityComparer<LocalRepository> {
            public bool Equals(LocalRepository x, LocalRepository y) {
                return x.Id == y.Id;
            }

            public int GetHashCode(LocalRepository x) {
                return x.Id;
            }
        }

        public bool Accepts (string key)
        {
            return key.StartsWith (this.RemoteFolder) && !key.Equals (this.RemoteFolder);
        }

        public bool Active { get; set; }
    }
}

