using System;
using NUnit.Framework;
using GreenQloud.Model;

namespace GreenQloud.Test
{
    [TestFixture()]
    public class RepositoryItemTest
    {
        [Test()]
        public void TestFullLocalName (){
         
            RepositoryItem item = new RepositoryItem();
            item.Name = "name";
            item.RelativePath = "relativePath";
            item.Repository = new LocalRepository("repo");

            Assert.AreEqual ("repo/relativePath/name", item.FullLocalName);
        }
    }
}

