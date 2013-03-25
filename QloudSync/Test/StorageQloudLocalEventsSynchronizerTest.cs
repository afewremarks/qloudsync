using System;
using NUnit.Framework;
using GreenQloud.Synchrony;
using System.IO;
using System.Threading;

namespace GreenQloud.Test
{
    [TestFixture()]
    public class StorageQloudLocalEventsSynchronizerTest 
    {
        [Test()]
        public void TestStop ()
        {
            StorageQloudLocalEventsSynchronizer localsynchronizer = StorageQloudLocalEventsSynchronizer.GetInstance();
            localsynchronizer.Start ();
            localsynchronizer.Stop ();
            Assert.AreEqual (System.Threading.ThreadState.Stopped, localsynchronizer.ControllerStatus);
        }

        [Test]
        public void TestRestart ()
        {
            StorageQloudLocalEventsSynchronizer localsynchronizer = StorageQloudLocalEventsSynchronizer.GetInstance();
            localsynchronizer.Start ();
            localsynchronizer.Pause ();
            localsynchronizer.Start ();
            Assert.True (localsynchronizer.Working);
        }
    }
}

