using System;
using NUnit.Framework;
using GreenQloud.Synchrony;

namespace GreenQloud.Test
{
    [TestFixture()]
    public class StorageQloudRemoteEventsSynchonizerTest
    {
        [Test()]
        public void TestStop ()
        {
            StorageQloudRemoteEventsSynchronizer remoteSynchronizer = StorageQloudRemoteEventsSynchronizer.GetInstance();
            remoteSynchronizer.Start ();
            remoteSynchronizer.Stop ();
            Assert.AreEqual (System.Threading.ThreadState.Stopped, remoteSynchronizer.ControllerStatus);
        }
        
        [Test]
        public void TestRestart ()
        {
            StorageQloudRemoteEventsSynchronizer remoteSynchronizer = StorageQloudRemoteEventsSynchronizer.GetInstance();
            remoteSynchronizer.Start ();
            remoteSynchronizer.Pause ();
            remoteSynchronizer.Start ();
            Assert.True (remoteSynchronizer.Working);
        }
    }
}

