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
            BacklogSynchronizer sync = new SimpleBacklogSynchronizer (logical, physical, remote, transfers, eventDAO);    
            
            RepositoryItem repoObj = new RepositoryItem();
            repoObj.Name = "teste.html";
            repoObj.RelativePath = "home";
            repoObj.Repo = new LocalRepository("...");
            logical.Create (repoObj);
            remote.Upload (repoObj);

            Event e = sync.GetEvent (repoObj, RepositoryType.REMOTE);
            Assert.AreEqual (RepositoryType.LOCAL, e.RepositoryType);
            Assert.AreEqual (EventType.DELETE, e.EventType);
        }

        [Test()]
        public void TestSynchronizingLocalItemDeletedAfterDisconnection (){
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController ();
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            EventDAO eventDAO = new SimpleEventDAO();
            BacklogSynchronizer sync = new SimpleBacklogSynchronizer (logical, physical, remote, transfers, eventDAO);

            RepositoryItem repoObj = new RepositoryItem();
            repoObj.Name = "teste.html";
            repoObj.RelativePath = "home";
            repoObj.Repo = new LocalRepository("...");
            logical.Create (repoObj);
            remote.Upload (repoObj);
            sync.Synchronize ();
            Assert.False (logical.Exists (repoObj));
            Assert.False (physical.Exists (repoObj));
            Assert.False (remote.Exists (repoObj));
        }


        [Test ()]
        public void TestGetEventRemoteItemDeleted (){
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController ();
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            EventDAO eventDAO = new SimpleEventDAO();
            BacklogSynchronizer sync = new SimpleBacklogSynchronizer (logical, physical, remote, transfers, eventDAO);

            RepositoryItem repoObj = new RepositoryItem();
            repoObj.Name = "teste.html";
            repoObj.RelativePath = "home";
            repoObj.Repo = new LocalRepository("...");
            logical.Create (repoObj);
            physical.Create (repoObj);

            Event e = sync.GetEvent (repoObj, RepositoryType.LOCAL);
            Assert.AreEqual (EventType.DELETE, e.EventType);
            Assert.AreEqual (RepositoryType.REMOTE, e.RepositoryType);
        }

        [Test ()]
        public void TestSynchronizingRemoteItemDeletedAfterDisconnection (){
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController ();
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            EventDAO eventDAO = new SimpleEventDAO();
            BacklogSynchronizer sync = new SimpleBacklogSynchronizer (logical, physical, remote, transfers, eventDAO);

            RepositoryItem repoObj = new RepositoryItem();
            repoObj.Name = "teste.html";
            repoObj.RelativePath = "home";
            repoObj.Repo = new LocalRepository("...");
            logical.Create (repoObj);
            physical.Create (repoObj);
            sync.Synchronize ();
          
            Assert.False (logical.Exists (repoObj));
            Assert.False (physical.Exists (repoObj));
            Assert.False (remote.Exists (repoObj));
        }

        [Test ()]
        public void TestGetEventLocalItemCreated (){
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController ();
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            EventDAO eventDAO = new SimpleEventDAO();
            BacklogSynchronizer sync = new SimpleBacklogSynchronizer (logical, physical, remote, transfers, eventDAO);
                        
            RepositoryItem repoObj = new RepositoryItem();
            repoObj.Name = "teste.html";
            repoObj.RelativePath = "home";
            repoObj.Repo = new LocalRepository("...");
            physical.Create (repoObj);
            
            Event e = sync.GetEvent (repoObj, RepositoryType.LOCAL);
            Assert.AreEqual (EventType.CREATE, e.EventType);
            Assert.AreEqual (RepositoryType.LOCAL, e.RepositoryType);
        }


        [Test ()]
        public void TestSynchronizingLocalItemCreatedAfterDisconnection (){
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController ();
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            EventDAO eventDAO = new SimpleEventDAO();
            BacklogSynchronizer sync = new SimpleBacklogSynchronizer (logical, physical, remote, transfers, eventDAO);    
            
            RepositoryItem repoObj = new RepositoryItem();
            repoObj.Name = "teste.html";
            repoObj.RelativePath = "home";
            repoObj.Repo = new LocalRepository("...");
            physical.Create (repoObj);
            sync.Synchronize ();
            
            Assert.True (logical.Exists (repoObj));
            Assert.True (physical.Exists (repoObj));
            Assert.True (remote.Exists (repoObj));
        }

        [Test ()]
        public void TestGetEventRemoteItemCreated (){
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController (physical);
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            EventDAO eventDAO = new SimpleEventDAO();
            BacklogSynchronizer sync = new SimpleBacklogSynchronizer (logical, physical, remote, transfers, eventDAO);
                        
            RepositoryItem repoObj = new RepositoryItem();
            repoObj.Name = "teste.html";
            repoObj.RelativePath = "home";
            repoObj.Repo = new LocalRepository("...");
            remote.Upload (repoObj);

            Event e = sync.GetEvent (repoObj, RepositoryType.REMOTE);
            Assert.AreEqual (EventType.CREATE, e.EventType);
            Assert.AreEqual (RepositoryType.REMOTE, e.RepositoryType);
        }

        [Test ()]
        public void TestSynchronizingRemoteItemCreatedAfterDisconnection (){
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController (physical);
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            EventDAO eventDAO = new SimpleEventDAO();
            BacklogSynchronizer sync = new SimpleBacklogSynchronizer (logical, physical, remote, transfers, eventDAO);

            RepositoryItem repoObj = new RepositoryItem();
            repoObj.Name = "teste.html";
            repoObj.RelativePath = "home";
            repoObj.Repo = new LocalRepository("...");
            remote.Upload (repoObj);
            sync.Synchronize ();
            
            Assert.True (logical.Exists (repoObj));
            Assert.True (physical.Exists (repoObj));
            Assert.True (remote.Exists (repoObj));
        }

        [Test ()]
        public void TestGetEventLocalUpdate (){
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController (physical);
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            EventDAO eventDAO = new SimpleEventDAO();
            BacklogSynchronizer sync = new SimpleBacklogSynchronizer (logical, physical, remote, transfers, eventDAO);

            RepositoryItem localItem = new RepositoryItem();
            localItem.Name = "teste.html";
            localItem.RelativePath = "home";
            localItem.Repo = new LocalRepository("***");
            localItem.TimeOfLastChange = DateTime.Now;
            localItem.MD5Hash = "123";
            
            RepositoryItem remoteItem = new RepositoryItem();
            remoteItem.Name = "teste.html";
            remoteItem.RelativePath = "home";
            remoteItem.Repo = new LocalRepository("***");
            remoteItem.TimeOfLastChange = DateTime.Now.Subtract(new TimeSpan(1,0,0));
            remoteItem.MD5Hash = "223";
            
            remote.Upload (remoteItem,"out of date");
            physical.Create (localItem,"updated");
            logical.Create (localItem,"out of date");

            Event e = sync.GetEvent (remoteItem, RepositoryType.REMOTE);
            Assert.AreEqual (EventType.UPDATE, e.EventType);
            Assert.AreEqual (RepositoryType.LOCAL, e.RepositoryType);
        }

        [Test ()]
        public void TestSynchronizingLocalUpdateAfterDisconnection (){
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController (physical);
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            EventDAO eventDAO = new SimpleEventDAO();
            BacklogSynchronizer sync = new SimpleBacklogSynchronizer (logical, physical, remote, transfers, eventDAO);

            RepositoryItem localItem = new RepositoryItem();
            localItem.Name = "teste.html";
            localItem.RelativePath = "home";
            localItem.Repo = new LocalRepository("***");
            localItem.TimeOfLastChange = DateTime.Now;
            
            RepositoryItem remoteItem = new RepositoryItem();
            remoteItem.Name = "teste.html";
            remoteItem.RelativePath = "home";
            remoteItem.Repo = new LocalRepository("***");
            remoteItem.TimeOfLastChange = DateTime.Now.Subtract(new TimeSpan(1,0,0));
            
            remote.Upload (remoteItem,"out of date");
            physical.Create (localItem,"updated");
            logical.Create (localItem,"out of date");
            sync.Synchronize ();
            
            Assert.AreEqual(physical.GetValue (localItem.FullLocalName),"updated");
            Assert.AreEqual(logical.GetValue (localItem.FullLocalName),"updated");           
        }


        [Test()]
        public void TestGetEventRemoteUpdate (){
            
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController (physical);
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            EventDAO eventDAO = new SimpleEventDAO();
            BacklogSynchronizer sync = new SimpleBacklogSynchronizer (logical, physical, remote, transfers, eventDAO);

            RepositoryItem localItem = new RepositoryItem();
            localItem.Name = "teste.html";
            localItem.RelativePath = "home";
            localItem.Repo = new LocalRepository("***");
            localItem.TimeOfLastChange = DateTime.Now.Subtract(new TimeSpan(1,0,0));
            localItem.MD5Hash = "123";
            
            RepositoryItem remoteItem = new RepositoryItem();
            remoteItem.Name = "teste.html";
            remoteItem.RelativePath = "home";
            remoteItem.Repo = new LocalRepository("***");
            remoteItem.TimeOfLastChange = DateTime.Now;
            remoteItem.MD5Hash = "223";

            remote.Upload (remoteItem,"updated");
            physical.Create (localItem,"out of date");
            logical.Create (localItem,"out of date");

            Event e = sync.GetEvent (remoteItem, RepositoryType.REMOTE);
            Assert.AreEqual (EventType.UPDATE, e.EventType);
            Assert.AreEqual (RepositoryType.REMOTE, e.RepositoryType);
        }

        [Test()]
        public void TestSynchronizingRemoteUpdateAfterDisconnection (){

            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController (physical);
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            EventDAO eventDAO = new SimpleEventDAO();
            BacklogSynchronizer sync = new SimpleBacklogSynchronizer (logical, physical, remote, transfers, eventDAO);

            RepositoryItem localItem = new RepositoryItem();
            localItem.Name = "teste.html";
            localItem.RelativePath = "home";
            localItem.Repo = new LocalRepository("***");
            localItem.TimeOfLastChange = DateTime.Now.Subtract(new TimeSpan(1,0,0));
            localItem.MD5Hash = "123";

            RepositoryItem remoteItem = new RepositoryItem();
            remoteItem.Name = "teste.html";
            remoteItem.RelativePath = "home";
            remoteItem.Repo = new LocalRepository("***");
            remoteItem.TimeOfLastChange = DateTime.Now;
            remoteItem.MD5Hash = "223";

            remote.Upload (remoteItem,"updated");
            physical.Create (localItem,"out of date");
            logical.Create (localItem,"out of date");
            sync.Synchronize ();

            Assert.AreEqual(physical.GetValue (localItem.FullLocalName),"updated");
            Assert.AreEqual(logical.GetValue (localItem.FullLocalName),"updated");           
        }

    }
}

