using GreenQloud;
using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Client.Embedded;
using Raven.Client.Indexes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QloudSyncCore.Core.Persistence
{
    public class DataDocumentStore
    {
        private static IDocumentStore instance;

        public static IDocumentStore Instance
        {
            get
            {
                if (instance == null)
                    throw new InvalidOperationException(
                      "IDocumentStore has not been initialized.");
                return instance;
            }
        }

        public static IDocumentStore Initialize()
        {
            instance = new EmbeddableDocumentStore { DataDirectory = RuntimeSettings.ConfigPath + Path.DirectorySeparatorChar + "qloudSync_db" };
            instance.Conventions.IdentityPartsSeparator = "-";
            instance.Initialize();
            return instance;
        }

        public static void Insert(Object o)
        {
            using (var session = instance.OpenSession())
            {
                session.Store(o);
                session.SaveChanges();
            }
        }

        public static int Count<T>()
        {
            using (var session = instance.OpenSession())
            {
                return session.Query<T>().Count();
            }
        }

        public static List<T> GetAll<T>()
        {
            using (var session = instance.OpenSession())
            {
                return session.Query<T>().ToList<T>();
            }
        }

        public static void Clear<T>()
        {
            using(var session = instance.OpenSession())
            {
                    session.Advanced.DocumentStore.DatabaseCommands.PutIndex("Raven/Repository", new IndexDefinitionBuilder<T>
                    {
                        Map = documents => documents.Select(entity => new { })
                    });

                    session.Advanced.DocumentStore.DatabaseCommands.DeleteByIndex("Raven/Repository", new IndexQuery());
            }
        
        }
            
    }
}
