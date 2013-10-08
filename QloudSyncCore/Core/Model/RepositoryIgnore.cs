using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using GreenQloud.Persistence;
using GreenQloud.Persistence.SQLite;
using GreenQloud.Repository;
using GreenQloud.Util;
using LitS3;
using System.Linq;

namespace GreenQloud.Model
{
    public class RepositoryIgnore
    {
        public RepositoryIgnore(LocalRepository localRepository)
        {
            this.Repository = localRepository;
        }
        public int Id {
            set; get;
        }

        public string Path{
            set; get;
        }

        public LocalRepository Repository
        {
            set;
            get;
        }
    }
}

