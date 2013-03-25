using System;
using NUnit.Framework;
using GreenQloud.Synchrony;
using System.Threading;

namespace GreenQloud.Test
{
    [TestFixture()]
    public class StorageQloudBacklogSynchronizerTest
    {
        [Test()]
        public void TestStop ()
        {
            StorageQloudBacklogSynchronizer backlogSynchronizer = StorageQloudBacklogSynchronizer.GetInstance();
            backlogSynchronizer.Start ();
            backlogSynchronizer.Stop ();
            Assert.AreEqual (System.Threading.ThreadState.Stopped, backlogSynchronizer.ControllerStatus);
        }
        
        [Test]
        public void TestRestart ()
        {
            StorageQloudBacklogSynchronizer backlogSynchronizer = StorageQloudBacklogSynchronizer.GetInstance();

            backlogSynchronizer.Start ();
            backlogSynchronizer.Stop ();
            backlogSynchronizer.Start ();
            Assert.True (backlogSynchronizer.Working);
        }
    }
}

