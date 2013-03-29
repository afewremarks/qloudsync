using System;
using NUnit.Framework;
using GreenQloud.Test.SimpleRepository;
using GreenQloud.Test.SimplePersistence;
using GreenQloud.Test.SimpleSynchrony;
using GreenQloud.Persistence;
using GreenQloud.Synchrony;
using GreenQloud.Model;

namespace GreenQloud.Test
{
    [TestFixture()]
    public class RemoteEventsSynchronizerTest
    {

        [Test()]
        public void TestAddCopyEvents ()
        {
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController ();
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            EventDAO eventDAO = new SimpleEventDAO();
            RemoteEventsSynchronizer sync = new SimpleRemoteEventsSynchronizer (logical, physical, remote, transfers, eventDAO); 
            
            RepositoryItem item = new RepositoryItem();
            item.Name = "teste.html";
            item.RelativePath = "home";
            item.Repository = new LocalRepository("...");
            item.RemoteMD5Hash = "123";
            remote.Upload (item);

            RepositoryItem item2 = new RepositoryItem();
            item2.Name = "teste2.html";
            item2.RelativePath = "home";
            item2.Repository = new LocalRepository("...");
            item2.RemoteMD5Hash = "123";
            physical.Create (item2);
            remote.Upload (item2);
            
            remote.RecentChangedItems(DateTime.Now).Add (item);
            
            sync.AddEvents ();
            
            Event e = eventDAO.EventsNotSynchronized [0];
            
            Assert.AreEqual (EventType.COPY, e.EventType);
            Assert.AreEqual (RepositoryType.REMOTE, e.RepositoryType);
        }

        [Test()]
        public void TestAddCreateEvents ()
        {
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController ();
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            EventDAO eventDAO = new SimpleEventDAO();
            RemoteEventsSynchronizer sync = new SimpleRemoteEventsSynchronizer (logical, physical, remote, transfers, eventDAO); 
            
            RepositoryItem item = new RepositoryItem();
            item.Name = "teste.html";
            item.RelativePath = "home";
            item.Repository = new LocalRepository("...");
            remote.Upload (item);
            
            remote.RecentChangedItems (DateTime.Now).Add (item);
            
            sync.AddEvents ();
            
            Event e = eventDAO.EventsNotSynchronized [0];
            
            Assert.AreEqual (EventType.CREATE, e.EventType);
            Assert.AreEqual (RepositoryType.REMOTE, e.RepositoryType);
        }

        [Test()]
        public void TestAddDeleteEvents ()
        {
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController ();
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            EventDAO eventDAO = new SimpleEventDAO();
            RemoteEventsSynchronizer sync = new SimpleRemoteEventsSynchronizer (logical, physical, remote, transfers, eventDAO); 
            
            RepositoryItem item = new RepositoryItem();
            item.Name = "teste.html";
            item.RelativePath = "home";
            item.Repository = new LocalRepository("...");
            physical.Create (item);

            sync.AddEvents ();

            Event e = eventDAO.EventsNotSynchronized [0];

            Assert.AreEqual (EventType.DELETE, e.EventType);
            Assert.AreEqual (RepositoryType.REMOTE, e.RepositoryType);
        }

        [Test()]
        public void TestAddMoveOrRenameEvents ()
        {
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController ();
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            EventDAO eventDAO = new SimpleEventDAO();
            RemoteEventsSynchronizer sync = new SimpleRemoteEventsSynchronizer (logical, physical, remote, transfers, eventDAO); 
            
            RepositoryItem item = new RepositoryItem();
            item.Name = "teste.html";
            item.RelativePath = "home";
            item.Repository = new LocalRepository("...");
            item.RemoteMD5Hash = "123";
            remote.Upload (item);
            
            RepositoryItem item2 = new RepositoryItem();
            item2.Name = "teste2.html";
            item2.RelativePath = "home";
            item2.Repository = new LocalRepository("...");
            item2.RemoteMD5Hash = "123";
            physical.Create (item2);

            
            remote.RecentChangedItems(DateTime.Now).Add (item);
            
            sync.AddEvents ();
            
            Event e = eventDAO.EventsNotSynchronized [0];
            
            Assert.AreEqual (EventType.MOVE_OR_RENAME, e.EventType);
            Assert.AreEqual (RepositoryType.REMOTE, e.RepositoryType);
        }

        [Test()]
        public void TestAddUpdateEvents ()
        {
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController ();
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            EventDAO eventDAO = new SimpleEventDAO();
            RemoteEventsSynchronizer sync = new SimpleRemoteEventsSynchronizer (logical, physical, remote, transfers, eventDAO); 
            
            RepositoryItem item = new RepositoryItem();
            item.Name = "teste.html";
            item.RelativePath = "home";
            item.Repository = new LocalRepository("...");
            item.RemoteMD5Hash = "123";
            remote.Upload (item);

            item.RemoteMD5Hash = "122";
            physical.Create (item);
            
            remote.RecentChangedItems(DateTime.Now).Add (item);
            
            sync.AddEvents ();
            
            Event e = eventDAO.EventsNotSynchronized [0];
            
            Assert.AreEqual (EventType.UPDATE, e.EventType);
            Assert.AreEqual (RepositoryType.REMOTE, e.RepositoryType);
        }

        
        [Test]
        public void TestFirst(){
            StorageQloudRemoteEventsSynchronizer sync = StorageQloudRemoteEventsSynchronizer.GetInstance();
            sync.FirstLoad();
        }
    }
}

