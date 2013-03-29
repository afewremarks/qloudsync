using System;
using NUnit.Framework;
using GreenQloud.Test.SimpleRepository;
using GreenQloud.Persistence;
using GreenQloud.Test.SimplePersistence;
using GreenQloud.Synchrony;
using GreenQloud.Test.SimpleSynchrony;
using GreenQloud.Model;

namespace GreenQloud.Test
{
    [TestFixture()]
    public class SynchronizerTest
    {
        [Test()]
        public void TestSynchronizingLocalItemDeleted (){
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController ();
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            EventDAO eventDAO = new SimpleEventDAO();
            AbstractBacklogSynchronizer sync = new SimpleBacklogSynchronizer (logical, physical, remote, transfers, eventDAO);
            sync.Working = true;
            RepositoryItem  item = new RepositoryItem();
             item.Name = "teste.html";
             item.RelativePath = "home";
             item.Repository = new LocalRepository("...");
            logical.Create ( item);
            remote.Upload ( item);
            sync.Synchronize ();
            Assert.False (logical.Exists ( item));
            Assert.False (physical.Exists ( item));
            Assert.False (remote.Exists ( item));
        }

        
        [Test ()]
        public void TestSynchronizingRemoteItemDeleted (){
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController ();
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            EventDAO eventDAO = new SimpleEventDAO();
            AbstractBacklogSynchronizer sync = new SimpleBacklogSynchronizer (logical, physical, remote, transfers, eventDAO);
            sync.Working = true;
            RepositoryItem  item = new RepositoryItem();
             item.Name = "teste.html";
             item.RelativePath = "home";
             item.Repository = new LocalRepository("...");
            logical.Create ( item);
            physical.Create ( item);
            sync.Synchronize ();
            
            Assert.False (logical.Exists ( item));
            Assert.False (physical.Exists ( item));
            Assert.False (remote.Exists ( item));
        }

        [Test ()]
        public void TestSynchronizingLocalItemCreated (){
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController ();
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            EventDAO eventDAO = new SimpleEventDAO();
            AbstractBacklogSynchronizer sync = new SimpleBacklogSynchronizer (logical, physical, remote, transfers, eventDAO);    
            sync.Working = true;
            RepositoryItem  item = new RepositoryItem();
             item.Name = "teste.html";
             item.RelativePath = "home";
             item.Repository = new LocalRepository("...");
            physical.Create ( item);
            sync.Working = true;
            sync.Synchronize ();
            
            Assert.True (logical.Exists ( item));
            Assert.True (physical.Exists ( item));
            Assert.True (remote.Exists ( item));
        }

        [Test ()]
        public void TestSynchronizingRemoteItemCreated (){
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController (physical);
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            EventDAO eventDAO = new SimpleEventDAO();
            AbstractBacklogSynchronizer sync = new SimpleBacklogSynchronizer (logical, physical, remote, transfers, eventDAO);
            sync.Working = true;
            RepositoryItem  item = new RepositoryItem();
             item.Name = "teste.html";
             item.RelativePath = "home";
             item.Repository = new LocalRepository("...");
            remote.Upload ( item);
            sync.Synchronize ();
            
            Assert.True (logical.Exists ( item));
            Assert.True (physical.Exists ( item));
            Assert.True (remote.Exists ( item));
        }
        

        [Test ()]
        public void TestSynchronizingLocalUpdate (){
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController (physical);
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            EventDAO eventDAO = new SimpleEventDAO();
            AbstractBacklogSynchronizer sync = new SimpleBacklogSynchronizer (logical, physical, remote, transfers, eventDAO);
            sync.Working = true;
            RepositoryItem localItem = new RepositoryItem();
            localItem.Name = "teste.html";
            localItem.RelativePath = "home";
            localItem.Repository = new LocalRepository("***");
            localItem.TimeOfLastChange = DateTime.Now;
            
            RepositoryItem remoteItem = new RepositoryItem();
            remoteItem.Name = "teste.html";
            remoteItem.RelativePath = "home";
            remoteItem.Repository = new LocalRepository("***");
            remoteItem.TimeOfLastChange = DateTime.Now.Subtract(new TimeSpan(1,0,0));
            
            remote.Upload (remoteItem,"out of date");
            physical.Create (localItem,"updated");
            logical.Create (localItem,"out of date");
            sync.Synchronize ();
            
            Assert.AreEqual(physical.GetValue (localItem.FullLocalName),"updated");
            Assert.AreEqual(logical.GetValue (localItem.FullLocalName),"updated");           
        }

        
        [Test()]
        public void TestSynchronizingRemoteUpdate (){
            
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController (physical);
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            EventDAO eventDAO = new SimpleEventDAO();
            AbstractBacklogSynchronizer sync = new SimpleBacklogSynchronizer (logical, physical, remote, transfers, eventDAO);
            sync.Working = true;
            RepositoryItem localItem = new RepositoryItem();
            localItem.Name = "teste.html";
            localItem.RelativePath = "home";
            localItem.Repository = new LocalRepository("***");
            localItem.TimeOfLastChange = DateTime.Now.Subtract(new TimeSpan(1,0,0));
            localItem.RemoteMD5Hash = "123";
            
            RepositoryItem remoteItem = new RepositoryItem();
            remoteItem.Name = "teste.html";
            remoteItem.RelativePath = "home";
            remoteItem.Repository = new LocalRepository("***");
            remoteItem.TimeOfLastChange = DateTime.Now;
            remoteItem.RemoteMD5Hash = "223";
            
            remote.Upload (remoteItem,"updated");
            physical.Create (localItem,"out of date");
            logical.Create (localItem,"out of date");
            sync.Synchronize ();
            
            Assert.AreEqual(physical.GetValue (localItem.FullLocalName),"updated");
            Assert.AreEqual(logical.GetValue (localItem.FullLocalName),"updated");           
        }

    }
}

