using System;
using GreenQloud.Persistence.SQLite;

namespace GreenQloud.Model
{
    public class LocalRepository
    {
        private static readonly SQLiteRepositoryDAO dao =  new SQLiteRepositoryDAO ();
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

        public bool Recovering {
            get;
            set;
        }
    }
}

