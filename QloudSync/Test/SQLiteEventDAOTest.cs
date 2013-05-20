using System;
using NUnit.Framework;
using System.IO;
using GreenQloud.Persistence;
using GreenQloud.Repository.Local;
using GreenQloud.Model;
using GreenQloud.Persistence.SQLite;
using System.Linq;

namespace GreenQloud.Test
{
    [TestFixture()]
    public class SQLiteEventDAOTest
    {
        [Test()]
        public void TestCreate ()
        {
            new SQLiteDatabase().CreateDataBase();
            SQLiteEventDAO eventDAO = new SQLiteEventDAO ();
                        
            if(!Directory.Exists (RuntimeSettings.HomePath))
                Directory.CreateDirectory (RuntimeSettings.HomePath);
            string testfile = Path.Combine (RuntimeSettings.HomePath, "local.html");
            if (!File.Exists (testfile)){               
                File.WriteAllText (testfile,"test file");
            }
            
            StorageQloudPhysicalRepositoryController physicalRepository = new StorageQloudPhysicalRepositoryController();
            RepositoryItem item = physicalRepository.CreateItemInstance (testfile);
            SQLiteRepositoryItemDAO repoDAO = new SQLiteRepositoryItemDAO();
            repoDAO.Create (item);

            Event e = new Event();
            e.EventType = EventType.DELETE;
            e.InsertTime = GlobalDateTime.Now.ToString();
            e.RepositoryType = RepositoryType.LOCAL;
            e.Item = item;
           
            eventDAO.Create (e);

            Assert.True(eventDAO.Exists(e));
        }

        [Test()]
        public void TestUpdateToSynchronized ()
        {
            SQLiteEventDAO eventDAO = new SQLiteEventDAO ();
            
            if(!Directory.Exists (RuntimeSettings.HomePath))
                Directory.CreateDirectory (RuntimeSettings.HomePath);
            string testfile = Path.Combine (RuntimeSettings.HomePath, "local.html");
            if (!File.Exists (testfile)){               
                File.WriteAllText (testfile,"test file");
            }
            
            StorageQloudPhysicalRepositoryController physicalRepository = new StorageQloudPhysicalRepositoryController();
            RepositoryItem item = physicalRepository.CreateItemInstance (testfile);
            SQLiteRepositoryItemDAO repoDAO = new SQLiteRepositoryItemDAO();
            repoDAO.Create (item);
            
            Event e = new Event();
            e.EventType = EventType.DELETE;
            e.InsertTime = GlobalDateTime.Now.ToString();
            e.RepositoryType = RepositoryType.LOCAL;
            e.Item = item;
            
            eventDAO.UpdateToSynchronized (e);
            
            Assert.False(eventDAO.EventsNotSynchronized.Any(ev => ev.EventType == e.EventType && ev.InsertTime == e.InsertTime && ev.RepositoryType == e.RepositoryType));
        }
        
        [Test()]
        public void TestEventsNotSynchronized() {
            SQLiteEventDAO eventDAO = new SQLiteEventDAO ();

            
            if(!Directory.Exists (RuntimeSettings.HomePath))
                Directory.CreateDirectory (RuntimeSettings.HomePath);
            string testfile = Path.Combine (RuntimeSettings.HomePath, "local.html");
            if (!File.Exists (testfile)){               
                File.WriteAllText (testfile,"test file");
            }
            
            StorageQloudPhysicalRepositoryController physicalRepository = new StorageQloudPhysicalRepositoryController();
            RepositoryItem item = physicalRepository.CreateItemInstance (testfile);
            SQLiteRepositoryItemDAO repoDAO = new SQLiteRepositoryItemDAO();
            repoDAO.Create (item);
            
            Event e = new Event();
            e.EventType = EventType.DELETE;
            e.InsertTime = GlobalDateTime.Now.ToString ();
            e.RepositoryType = RepositoryType.LOCAL;
            e.Item = item;
            
            eventDAO.Create (e);
            
            Assert.True(eventDAO.EventsNotSynchronized.Count>0);
        }

        [Test()]
        public void TestExistsConflict ()
        {
            SQLiteEventDAO eventDAO = new SQLiteEventDAO ();

            if(!Directory.Exists (RuntimeSettings.HomePath))
                Directory.CreateDirectory (RuntimeSettings.HomePath);
            string testfile = Path.Combine (RuntimeSettings.HomePath, "local.html");
            if (!File.Exists (testfile)){               
                File.WriteAllText (testfile,"test file");
            }
            
            StorageQloudPhysicalRepositoryController physicalRepository = new StorageQloudPhysicalRepositoryController();
            RepositoryItem item = physicalRepository.CreateItemInstance (testfile);
            SQLiteRepositoryItemDAO repoDAO = new SQLiteRepositoryItemDAO();
            repoDAO.Create (item);
            
            Event e = new Event();
            e.EventType = EventType.DELETE;
            e.InsertTime = GlobalDateTime.Now.ToString();
            e.RepositoryType = RepositoryType.LOCAL;
            e.Item = item;
            
            eventDAO.Create (e);

            e.RepositoryType = RepositoryType.REMOTE;
           
            
            Assert.True(eventDAO.ExistsConflict(e));  
        }

        [Test()]
        public void TestNotExistsConflict ()
        {
            SQLiteEventDAO eventDAO = new SQLiteEventDAO ();
            
            if(!Directory.Exists (RuntimeSettings.HomePath))
                Directory.CreateDirectory (RuntimeSettings.HomePath);
            string testfile = Path.Combine (RuntimeSettings.HomePath, "local.html");
            if (!File.Exists (testfile)){               
                File.WriteAllText (testfile,"test file");
            }
            
            StorageQloudPhysicalRepositoryController physicalRepository = new StorageQloudPhysicalRepositoryController();
            RepositoryItem item = physicalRepository.CreateItemInstance (testfile);
            SQLiteRepositoryItemDAO repoDAO = new SQLiteRepositoryItemDAO();
            repoDAO.Create (item);
            
            Event e = new Event();
            e.EventType = EventType.DELETE;
            e.InsertTime = GlobalDateTime.Now.ToString();
            e.RepositoryType = RepositoryType.LOCAL;
            e.Item = item;
            
            eventDAO.Create (e);
            
            
            Assert.False(eventDAO.ExistsConflict(e));  
        }


    }
}

