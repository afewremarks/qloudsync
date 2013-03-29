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
            backlogSynchronizer.Pause ();
            backlogSynchronizer.Stop ();
            Assert.AreEqual (System.Threading.ThreadState.Stopped, backlogSynchronizer.ControllerStatus);
        }
        
        [Test]
        public void TestsRestart ()
        {
//            StorageQloudBacklogSynchronizer backlogSynchronizer = StorageQloudBacklogSynchronizer.GetInstance();
//            backlogSynchronizer.Start ();
//            backlogSynchronizer.Pause ();
//            backlogSynchronizer.Start ();
//            Assert.True (backlogSynchronizer.Working);

        }
    }
}

