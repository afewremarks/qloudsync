using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using GreenQloud.Repository;
using GreenQloud.Util;
using LitS3;
using System.Linq;

namespace GreenQloud.Model
{
    public class RepositoryIgnore
    {
        public RepositoryIgnore(LocalRepository localRepository, string path = null)
        {
            this.Repository = localRepository;
            this.Path = path;
        }
        public int Id {
            set; get;
        }

        public string Path{
            set; get;
        }

        private LocalRepository repository;
        public LocalRepository Repository
        {
            set {
                repository = value;
                if (repository != null)
                    RepositoryId = repository.Id;
            }
            get {
                return repository;
            }
        }

        public int RepositoryId { get; set; }
    }
}

