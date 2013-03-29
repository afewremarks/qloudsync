using System;
using NUnit.Framework;
using GreenQloud.Persistence.SQLite;
using System.IO;
using GreenQloud.Repository.Local;
using GreenQloud.Model;

namespace GreenQloud.Test
{
    [TestFixture()]
    public class SQLiteRepositoryItemDAOTest
    {
        [Test()]
        public void TestCase ()
        {
            SQLiteTransferDAO transferDAO = new SQLiteTransferDAO ();
            
            string testfile = Path.Combine (RuntimeSettings.HomePath, "local.html");
            if (!File.Exists (testfile)){               
                File.WriteAllText (testfile,"test file");
            }
            
            StorageQloudPhysicalRepositoryController physicalRepository = new StorageQloudPhysicalRepositoryController();
            RepositoryItem item = physicalRepository.CreateItemInstance (testfile);
            SQLiteRepositoryItemDAO repoDAO = new SQLiteRepositoryItemDAO();
            repoDAO.Create (item);
            Assert.True(repoDAO.Exists(item));
        }

        public void TestRemove ()
        {
            SQLiteTransferDAO transferDAO = new SQLiteTransferDAO ();
            
            string testfile = Path.Combine (RuntimeSettings.HomePath, "local.html");
            if (!File.Exists (testfile)){               
                File.WriteAllText (testfile,"test file");
            }
            
            StorageQloudPhysicalRepositoryController physicalRepository = new StorageQloudPhysicalRepositoryController();
            RepositoryItem item = physicalRepository.CreateItemInstance (testfile);
            SQLiteRepositoryItemDAO repoDAO = new SQLiteRepositoryItemDAO();
            repoDAO.Remove (item);
            Assert.False(repoDAO.Exists(item));
        }

    }
}

