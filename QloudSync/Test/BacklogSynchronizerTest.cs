using System;
using NUnit.Framework;
using GreenQloud.Synchrony;
using System.IO;
using GreenQloud.Test.SimpleRepository;
using GreenQloud.Persistence;
using GreenQloud.Repository.Local;
using GreenQloud.Model;
using GreenQloud.Test.SimpleSynchrony;
using GreenQloud.Test.SimplePersistence;


namespace GreenQloud.Test
{
    [TestFixture()]
    public class BacklogSynchronizerTest 
    {
        [Test()]
        public void TestGetEventLocalItemDeleted (){
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController ();
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            EventDAO eventDAO = new SimpleEventDAO();
            AbstractBacklogSynchronizer sync = new SimpleBacklogSynchronizer (logical, physical, remote, transfers, eventDAO);    
            
            RepositoryItem  item = new RepositoryItem();
             item.Name = "teste.html";
             item.RelativePath = "home";
             item.Repository = new LocalRepository("...");
            logical.Create ( item);
            remote.Upload ( item);

            Event e = sync.GetEvent ( item, RepositoryType.REMOTE);
            Assert.AreEqual (RepositoryType.LOCAL, e.RepositoryType);
            Assert.AreEqual (EventType.DELETE, e.EventType);
        }

        [Test ()]
        public void TestGetEventRemoteItemDeleted (){
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController ();
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            EventDAO eventDAO = new SimpleEventDAO();
            AbstractBacklogSynchronizer sync = new SimpleBacklogSynchronizer (logical, physical, remote, transfers, eventDAO);

            RepositoryItem  item = new RepositoryItem();
             item.Name = "teste.html";
             item.RelativePath = "home";
             item.Repository = new LocalRepository("...");
            logical.Create ( item);
            physical.Create ( item);

            Event e = sync.GetEvent ( item, RepositoryType.LOCAL);
            Assert.AreEqual (EventType.DELETE, e.EventType);
            Assert.AreEqual (RepositoryType.REMOTE, e.RepositoryType);
        }


        [Test ()]
        public void TestGetEventLocalItemCreated (){
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController ();
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            EventDAO eventDAO = new SimpleEventDAO();
            AbstractBacklogSynchronizer sync = new SimpleBacklogSynchronizer (logical, physical, remote, transfers, eventDAO);
                        
            RepositoryItem  item = new RepositoryItem();
             item.Name = "teste.html";
             item.RelativePath = "home";
             item.Repository = new LocalRepository("...");
            physical.Create ( item);
            
            Event e = sync.GetEvent ( item, RepositoryType.LOCAL);
            Assert.AreEqual (EventType.CREATE, e.EventType);
            Assert.AreEqual (RepositoryType.LOCAL, e.RepositoryType);
        }


        [Test ()]
        public void TestGetEventRemoteItemCreated (){
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController (physical);
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            EventDAO eventDAO = new SimpleEventDAO();
            AbstractBacklogSynchronizer sync = new SimpleBacklogSynchronizer (logical, physical, remote, transfers, eventDAO);
                        
            RepositoryItem  item = new RepositoryItem();
             item.Name = "teste.html";
             item.RelativePath = "home";
             item.Repository = new LocalRepository("...");
            remote.Upload ( item);

            Event e = sync.GetEvent ( item, RepositoryType.REMOTE);
            Assert.AreEqual (EventType.CREATE, e.EventType);
            Assert.AreEqual (RepositoryType.REMOTE, e.RepositoryType);
        }

        [Test ()]
        public void TestGetEventLocalUpdate (){
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController (physical);
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            EventDAO eventDAO = new SimpleEventDAO();
            AbstractBacklogSynchronizer sync = new SimpleBacklogSynchronizer (logical, physical, remote, transfers, eventDAO);

            RepositoryItem localItem = new RepositoryItem();
            localItem.Name = "teste.html";
            localItem.RelativePath = "home";
            localItem.Repository = new LocalRepository("***");
            localItem.TimeOfLastChange = DateTime.Now;
            localItem.LocalMD5Hash = "123";
            
            RepositoryItem remoteItem = new RepositoryItem();
            remoteItem.Name = "teste.html";
            remoteItem.RelativePath = "home";
            remoteItem.Repository = new LocalRepository("***");
            remoteItem.TimeOfLastChange = DateTime.Now.Subtract(new TimeSpan(1,0,0));
            remoteItem.RemoteMD5Hash = "223";
            
            remote.Upload (remoteItem,"out of date");
            physical.Create (localItem,"updated");
            logical.Create (localItem,"out of date");

            Event e = sync.GetEvent (remoteItem, RepositoryType.REMOTE);
            Assert.AreEqual (EventType.UPDATE, e.EventType);
            Assert.AreEqual (RepositoryType.LOCAL, e.RepositoryType);
        }

        [Test()]
        public void TestGetEventRemoteUpdate (){
            
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController (physical);
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            EventDAO eventDAO = new SimpleEventDAO();
            AbstractBacklogSynchronizer sync = new SimpleBacklogSynchronizer (logical, physical, remote, transfers, eventDAO);

            RepositoryItem localItem = new RepositoryItem();
            localItem.Name = "teste.html";
            localItem.RelativePath = "home";
            localItem.Repository = new LocalRepository("***");
            localItem.TimeOfLastChange = DateTime.Now.Subtract(new TimeSpan(1,0,0));
            localItem.LocalMD5Hash = "123";
            
            RepositoryItem remoteItem = new RepositoryItem();
            remoteItem.Name = "teste.html";
            remoteItem.RelativePath = "home";
            remoteItem.Repository = new LocalRepository("***");
            remoteItem.TimeOfLastChange = DateTime.Now;
            remoteItem.RemoteMD5Hash = "223";

            remote.Upload (remoteItem,"updated");
            physical.Create (localItem,"out of date");
            logical.Create (localItem,"out of date");

            Event e = sync.GetEvent (remoteItem, RepositoryType.REMOTE);
            Assert.AreEqual (EventType.UPDATE, e.EventType);
            Assert.AreEqual (RepositoryType.REMOTE, e.RepositoryType);
        }

    }
}

