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

        [Test()]
        public void TestCreateInstance_LocalFolder(){
            string fullpath = "/home/folder/test";
            RepositoryItem item = RepositoryItem.CreateInstance (new LocalRepository("/home"), fullpath, true, 0, new DateTime());
            Assert.AreEqual (fullpath, item.FullLocalName);
        }

        [Test()]
        public void TestCreateInstance_LocalFolder_InRoot(){
            string fullpath = "test";
            RepositoryItem item = RepositoryItem.CreateInstance (new LocalRepository("/home"), fullpath, true, 0, new DateTime());
            Assert.AreEqual (fullpath, item.FullLocalName);
        }

        [Test()]
        public void TestCreateInstance_RemoteFolder_OutOfTrashAndInRoot(){
            string fullpath = "home/test/";
            RepositoryItem item = RepositoryItem.CreateInstance (new LocalRepository("/home"), fullpath, true, 0, new DateTime());
            Assert.AreEqual ("/home/test", item.FullLocalName);
        }

        [Test()]
        public void TestCreateInstance_RemoteFolder_OutOfTrash(){
            string fullpath = "home/folder/test/";
            RepositoryItem item = RepositoryItem.CreateInstance (new LocalRepository("/home"), fullpath, true, 0, new DateTime());
            Assert.AreEqual ("/home/folder/test", item.FullLocalName);
        }

        [Test()]
        public void TestCreateInstance_RemoteFolder_InTrashRoot(){
            string fullpath = "home/.trash/test/";
            RepositoryItem item = RepositoryItem.CreateInstance (new LocalRepository("/home"), fullpath, true, 0, new DateTime());
            Assert.AreEqual ("/home/test", item.FullLocalName);
        }

        [Test()]
        public void TestCreateInstance_RemoteFolder_InTrash(){
            string fullpath = "home/.trash/folder/test/";
            RepositoryItem item = RepositoryItem.CreateInstance (new LocalRepository("/home"), fullpath, true, 0, new DateTime());
            Assert.AreEqual ("/home/folder/test", item.FullLocalName);
        }


        [Test()]
        public void TestCreateInstance_LocalFile(){
            string fullpath = "/home/folder/test.html";
            RepositoryItem item = RepositoryItem.CreateInstance (new LocalRepository("/home"), fullpath, false, 0, new DateTime());
            Assert.AreEqual (fullpath, item.FullLocalName);
        }
        
        [Test()]
        public void TestCreateInstance_LocalFile_InRoot(){
            string fullpath = "/home/test.html";
            RepositoryItem item = RepositoryItem.CreateInstance (new LocalRepository("/home"), fullpath, false, 0, new DateTime());
            Assert.AreEqual (fullpath, item.FullLocalName);
        }
        
        [Test()]
        public void TestCreateInstance_RemoteFile_OutOfTrashAndInRoot(){
            string fullpath = "test.html";
            RepositoryItem item = RepositoryItem.CreateInstance (new LocalRepository("/home"), fullpath, false, 0, new DateTime());
            Assert.AreEqual ("/home/test.html", item.FullLocalName);
        }
        
        [Test()]
        public void TestCreateInstance_RemoteFile_OutOfTrash(){
            string fullpath = "home/folder/test.html";
            RepositoryItem item = RepositoryItem.CreateInstance (new LocalRepository("/home"), fullpath, false, 0, new DateTime());
            Assert.AreEqual ("/home/folder/test.html", item.FullLocalName);
        }
        
        [Test()]
        public void TestCreateInstance_RemoteFile_InTrashRoot(){
            string fullpath = "home/.trash/test.html";
            RepositoryItem item = RepositoryItem.CreateInstance (new LocalRepository("/home"), fullpath, false, 0, new DateTime());
            Assert.AreEqual ("/home/test.html", item.FullLocalName);
        }
        
        [Test()]
        public void TestCreateInstance_RemoteFile_InTrash(){
            string fullpath = "home/.trash/folder/test.html";
            RepositoryItem item = RepositoryItem.CreateInstance (new LocalRepository("/home"), fullpath, false, 0, new DateTime());
            Assert.AreEqual ("/home/folder/test.html", item.FullLocalName);
        }
    }
}

