using System;
using NUnit.Framework;
using GreenQloud.Persistence.SQLite;
using System.IO;
using GreenQloud.Model;
using Mono.Data.Sqlite;

namespace GreenQloud.Test
{
    [TestFixture()]
    public class StorageQloudRepositoryDAOTest
    {
        [Test()]
        public void TestDelete ()
        {            
            SQLiteRepositoryDAO dao = new SQLiteRepositoryDAO();
            dao.DeleteAll();
            Assert.True (dao.All.Count == 0);
        }

        [Test()]
        public void TestInsert ()
        {            
            SQLiteRepositoryDAO dao = new SQLiteRepositoryDAO();
            LocalRepository repo = new LocalRepository(RuntimeSettings.HomePath);
            dao.DeleteAll();
            dao.Create(repo);
            Assert.True (dao.All.Count > 0);
        }
        
        [Test()]
        public void TestAll(){
            SQLiteRepositoryDAO dao = new SQLiteRepositoryDAO();
            LocalRepository repo = new LocalRepository(RuntimeSettings.HomePath);
            dao.Create(repo);
            Assert.True (dao.All.Count > 0);
        }

        [Test()]
        public void TestGetRepositoryByItemFullName ()
        {            
            SQLiteRepositoryDAO dao = new SQLiteRepositoryDAO();
            LocalRepository repo = new LocalRepository(RuntimeSettings.HomePath);
            dao.Create(repo);
            Assert.AreEqual (dao.GetRepositoryByItemFullName (Path.Combine(RuntimeSettings.HomePath, "teste/")).Path, RuntimeSettings.HomePath);
        }

    }
}

