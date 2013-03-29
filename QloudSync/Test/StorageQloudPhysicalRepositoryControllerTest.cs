using System;
using NUnit.Framework;
using GreenQloud.Repository.Local;
using GreenQloud.Model;
using System.IO;

namespace GreenQloud.Test
{
    [TestFixture()]
    public class StorageQloudPhysicalRepositoryControllerTest
    {
        [Test()]
        public void TestCreateInstanceFile ()
        {
            StorageQloudPhysicalRepositoryController physicalController = new StorageQloudPhysicalRepositoryController();
            RepositoryItem item = physicalController.CreateItemInstance(Path.Combine(RuntimeSettings.HomePath,"teste.html"));
            Assert.AreEqual ("teste.html", item.Name);
            Assert.AreEqual ("",item.RelativePath);
            Assert.AreEqual (RuntimeSettings.HomePath, item.Repository.Path);
            Assert.AreEqual (RuntimeSettings.DefaultBucketName, item.RelativePathInBucket);
           
        }
    }
}

