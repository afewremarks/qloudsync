using System;

namespace GreenQloud.Model
{
    public class LocalRepository
    {
        public LocalRepository (string path)
        {
            Path = path;
        }

        public static LocalRepository CreateInstance (int id)
        {
            throw new NotImplementedException ();
        }

        public string Id {
            get;
            set;
        }

        public string Path {
            get;
            set;
        }

    }
}

