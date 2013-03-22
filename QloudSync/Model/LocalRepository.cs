using System;

namespace GreenQloud.Model
{
    public class LocalRepository
    {
        public LocalRepository (string path)
        {
            Path = path;
        }

        public string Path {
            get;
            set;
        }
    }
}

