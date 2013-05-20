using System;
using NUnit.Framework;
using GreenQloud.Model;
using GreenQloud.Repository.Local;
using System.IO;
using GreenQloud.Persistence.SQLite;

namespace GreenQloud.Test
{
    [TestFixture()]
    public class SQLiteTransferDAOTest
    {
        [Test()]
        public void TestCreate ()
        {
            SQLiteTransferDAO transferDAO = new SQLiteTransferDAO ();

            string testfile = Path.Combine (RuntimeSettings.HomePath, "local.html");
            if (!File.Exists (testfile)){               
                File.WriteAllText (testfile,"test file");
            }
            
            StorageQloudPhysicalRepositoryController physicalRepository = new StorageQloudPhysicalRepositoryController();
            RepositoryItem item = physicalRepository.CreateItemInstance (testfile);

            Transfer transfer = new Transfer();
            transfer.InitialTime = GlobalDateTime.Now;
            transfer.Type = TransferType.DOWNLOAD;
            transfer.Item = item;
            transfer.EndTime = GlobalDateTime.Now;
            SQLiteRepositoryItemDAO repoDAO = new SQLiteRepositoryItemDAO();
            repoDAO.Create (item);
            transferDAO.Create (transfer);
            Assert.True (transferDAO.Exists(transfer));
        }
    }
}

