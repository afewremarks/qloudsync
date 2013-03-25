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
            StorageQloudRemoteEventsSynchronizer localsynchronizer = StorageQloudRemoteEventsSynchronizer.GetInstance();
            localsynchronizer.Start ();
            localsynchronizer.Stop ();
            Assert.AreEqual (System.Threading.ThreadState.Stopped, localsynchronizer.ControllerStatus);
        }
        
        [Test]
        public void TestRestart ()
        {
            StorageQloudRemoteEventsSynchronizer localsynchronizer = StorageQloudRemoteEventsSynchronizer.GetInstance();
            localsynchronizer.Start ();
            localsynchronizer.Pause ();
            localsynchronizer.Start ();
            Assert.True (localsynchronizer.Working);
        }
    }
}

