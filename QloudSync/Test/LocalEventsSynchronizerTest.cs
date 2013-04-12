using System;
using NUnit.Framework;
using GreenQloud.Test.SimpleRepository;
using GreenQloud.Test.SimplePersistence;
using GreenQloud.Synchrony;
using GreenQloud.Test.SimpleSynchrony;
using GreenQloud.Persistence;
using GreenQloud.Model;

namespace GreenQloud.Test
{
    [TestFixture()]
    public class LocalEventsSynchronizerTest
    {
//        [Test()]
//        public void TestGetEventsCreate ()
//        {
//            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
//            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
//            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController ();
//            logical.PhysicalController = physical;
//            TransferDAO transfers = new SimpleTransferDAO ();
//            EventDAO eventDAO = new SimpleEventDAO();
//            AbstractLocalEventsSynchronizer sync = new SimpleLocalEventsSynchronizer (logical, physical, remote, transfers, eventDAO); 
//
//            RepositoryItem item = new RepositoryItem();
//            item.Name = "teste.html";
//            item.RelativePath = "home";
//            item.Repository = new LocalRepository("...");
//            physical.Create (item);
//
//            Event e = sync.GetEventType (item);
//
//            Assert.AreEqual (EventType.CREATE, e.EventType);
//            Assert.AreEqual (RepositoryType.LOCAL, e.RepositoryType);
//        }
//
//        [Test()]
//        public void TestSynchronizeItem ()
//        {
//            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
//            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
//            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController ();
//            logical.PhysicalController = physical;
//            TransferDAO transfers = new SimpleTransferDAO ();
//            EventDAO eventDAO = new SimpleEventDAO();
//            AbstractLocalEventsSynchronizer sync = new SimpleLocalEventsSynchronizer (logical, physical, remote, transfers, eventDAO); 
//            sync.Start ();
//            RepositoryItem item = new RepositoryItem();
//            item.Name = "teste.html";
//            item.RelativePath = "home";
//            item.Repository = new LocalRepository("...");
//            physical.Create (item); 
//            sync.Synchronize (item);
//            System.Threading.Thread.Sleep(1000);
//            
//            Assert.True (logical.Exists (item));
//            Assert.True (physical.Exists (item));
//            Assert.True (remote.Exists (item));
//        }
//
//        [Test()]
//        public void TestGetEventsDelete(){
//
//            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
//            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
//            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController ();
//            logical.PhysicalController = physical;
//            TransferDAO transfers = new SimpleTransferDAO ();
//            EventDAO eventDAO = new SimpleEventDAO();
//            AbstractLocalEventsSynchronizer sync = new SimpleLocalEventsSynchronizer (logical, physical, remote, transfers, eventDAO); 
//            
//            RepositoryItem item = new RepositoryItem();
//            item.Name = "teste.html";
//            item.RelativePath = "home";
//            item.Repository = new LocalRepository("...");
//            
//            Event e = sync.GetEventType (item);
//            
//            Assert.AreEqual (EventType.DELETE, e.EventType);
//            Assert.AreEqual (RepositoryType.LOCAL, e.RepositoryType);
//        }
//
//        [Test()]
//        public void TestGetEventsRename(){
//            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
//            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
//            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController ();
//            logical.PhysicalController = physical;
//            TransferDAO transfers = new SimpleTransferDAO ();
//            EventDAO eventDAO = new SimpleEventDAO();
//            AbstractLocalEventsSynchronizer sync = new SimpleLocalEventsSynchronizer (logical, physical, remote, transfers, eventDAO); 
//            
//            RepositoryItem item = new RepositoryItem();
//            item.Name = "teste.html";
//            item.RelativePath = "home";
//            item.Repository = new LocalRepository("...");
//            item.LocalMD5Hash = "123";
//            physical.Create (item, "same text");
//
//
//            RepositoryItem item2 = new RepositoryItem();
//            item2.Name = "teste2.html";
//            item2.RelativePath = "home";
//            item2.Repository = new LocalRepository("...");
//            item2.LocalMD5Hash = "123";
//            remote.Upload (item2, "same text");
//            
//            Event e = sync.GetEventType (item);
//            
//            Assert.AreEqual (EventType.MOVE_OR_RENAME, e.EventType);
//            Assert.AreEqual (RepositoryType.LOCAL, e.RepositoryType); 
//        }
//
//        [Test()]
//        public void TestGetEventsUpdate(){
//            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
//            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
//            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController ();
//            logical.PhysicalController = physical;
//            TransferDAO transfers = new SimpleTransferDAO ();
//            EventDAO eventDAO = new SimpleEventDAO();
//            AbstractLocalEventsSynchronizer sync = new SimpleLocalEventsSynchronizer (logical, physical, remote, transfers, eventDAO); 
//            
//            RepositoryItem item = new RepositoryItem();
//            item.Name = "teste.html";
//            item.RelativePath = "home";
//            item.Repository = new LocalRepository("...");
//            physical.Create (item, "updated");
//
//            remote.Upload (item, "out of date");
//            
//            Event e = sync.GetEventType (item);
//            
//            Assert.AreEqual (EventType.UPDATE, e.EventType);
//            Assert.AreEqual (RepositoryType.LOCAL, e.RepositoryType);        
//        }

    }
}

