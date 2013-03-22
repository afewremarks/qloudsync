using System;
using NUnit.Framework;
using GreenQloud.Synchrony;
using System.IO;
using GreenQloud.Test.SimpleRepository;
using GreenQloud.Persistence;
using GreenQloud.Repository.Local;
using GreenQloud.Repository.Model;


namespace GreenQloud.Test
{
    [TestFixture()]
    public class BacklogSynchronizerTest 
    {
        [Test()]
        public void TestSynchronizingLocalObjectDeletedAfterDisconnection (){
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController ();
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            BacklogSynchronizer sync = new SimpleBacklogSynchronizer (logical, physical, remote, transfers);    

            RepoObject repoObj = new RepoObject();
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
        public void TestSynchronizingRemoteObjectDeletedAfterDisconnection (){
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController ();
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            BacklogSynchronizer sync = new SimpleBacklogSynchronizer (logical, physical, remote, transfers);    
            
            RepoObject repoObj = new RepoObject();
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
        public void TestSynchronizingLocalObjectCreatedAfterDisconnection (){
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController ();
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            BacklogSynchronizer sync = new SimpleBacklogSynchronizer (logical, physical, remote, transfers);    
            
            RepoObject repoObj = new RepoObject();
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
        public void TestSynchronizingRemoteObjectCreatedAfterDisconnection (){
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController (physical);
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            BacklogSynchronizer sync = new SimpleBacklogSynchronizer (logical, physical, remote, transfers);    
            
            RepoObject repoObj = new RepoObject();
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
        public void TestSynchronizingLocalUpdateAfterDisconnection (){
            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController (physical);
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            BacklogSynchronizer sync = new SimpleBacklogSynchronizer (logical, physical, remote, transfers);    
            
            RepoObject localObject = new RepoObject();
            localObject.Name = "teste.html";
            localObject.RelativePath = "home";
            localObject.Repo = new LocalRepository("***");
            localObject.TimeOfLastChange = DateTime.Now;
            
            RepoObject remoteObject = new RepoObject();
            remoteObject.Name = "teste.html";
            remoteObject.RelativePath = "home";
            remoteObject.Repo = new LocalRepository("***");
            remoteObject.TimeOfLastChange = DateTime.Now.Subtract(new TimeSpan(1,0,0));
            
            remote.Upload (remoteObject,"out of date");
            physical.Create (localObject,"updated");
            logical.Create (localObject,"out of date");
            sync.Synchronize ();
            
            Assert.AreEqual(physical.GetValue (localObject.FullLocalName),"updated");
            Assert.AreEqual(logical.GetValue (localObject.FullLocalName),"updated");           
        }


        [Test()]
        public void TestSynchronizingRemoteUpdateAfterDisconnection (){

            SimpleLogicalRepositoryController logical = new SimpleLogicalRepositoryController ();
            SimplePhysicalRepositoryController physical = new SimplePhysicalRepositoryController (); 
            SimpleRemoteRepositoryController remote = new SimpleRemoteRepositoryController (physical);
            logical.PhysicalController = physical;
            TransferDAO transfers = new SimpleTransferDAO ();
            BacklogSynchronizer sync = new SimpleBacklogSynchronizer (logical, physical, remote, transfers);    

            RepoObject localObject = new RepoObject();
            localObject.Name = "teste.html";
            localObject.RelativePath = "home";
            localObject.Repo = new LocalRepository("***");
            localObject.TimeOfLastChange = DateTime.Now.Subtract(new TimeSpan(1,0,0));

            RepoObject remoteObject = new RepoObject();
            remoteObject.Name = "teste.html";
            remoteObject.RelativePath = "home";
            remoteObject.Repo = new LocalRepository("***");
            remoteObject.TimeOfLastChange = DateTime.Now;

            remote.Upload (remoteObject,"updated");
            physical.Create (localObject,"out of date");
            logical.Create (localObject,"out of date");
            sync.Synchronize ();

            Assert.AreEqual(physical.GetValue (localObject.FullLocalName),"updated");
            Assert.AreEqual(logical.GetValue (localObject.FullLocalName),"updated");           
        }

    }
}

