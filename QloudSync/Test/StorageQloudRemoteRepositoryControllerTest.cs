using System;
using NUnit.Framework;
using GreenQloud.Repository.Remote;
using Amazon.S3.Model;
using Amazon.S3;
using GreenQloud.Model;
using System.IO;
using GreenQloud.Repository.Local;

namespace GreenQloud.Test
{
    [TestFixture()]
    public class StorageQloudRemoteRepositoryControllerTest
    {
        [Test()]
        public void TestUpdateVersion ()
        {
            StorageQloudRemoteRepositoryController repositoryController = new StorageQloudRemoteRepositoryController();
            Assert.AreEqual("name(1)", repositoryController.UpdateVersionName("name(0)"));
        }

        [Test]
        public void TestDownloadFolder(){
            StorageQloudRemoteRepositoryController repositoryController = new StorageQloudRemoteRepositoryController();

            RepositoryItem item = new RepositoryItem();
            item.Name = "Teste/";
            item.IsAFolder = true;
            item.RelativePath = "";
            item.Repository = new LocalRepository (Path.Combine(RuntimeSettings.HomePath,"testing"));
            repositoryController.Download(item);

            StorageQloudPhysicalRepositoryController physicalController = new StorageQloudPhysicalRepositoryController();

            Assert.True (physicalController.Exists (item));
        }

        [Test]
        public void TestDownloadFile(){
            StorageQloudRemoteRepositoryController repositoryController = new StorageQloudRemoteRepositoryController();
            
            RepositoryItem item = new RepositoryItem();
            item.Name = "teste.html";
            item.RelativePath = "";
            item.Repository = new LocalRepository (Path.Combine(RuntimeSettings.HomePath,"testing"));
            if (!repositoryController.Exists (item)){
                string testfile = Path.Combine (RuntimeSettings.HomePath, "teste.html");
                if (!File.Exists (testfile)){               
                    File.WriteAllText (testfile,"test file");
                }
                repositoryController.Upload(item);
                File.Delete (testfile);
            }
            repositoryController.Download(item);
            
            StorageQloudPhysicalRepositoryController physicalController = new StorageQloudPhysicalRepositoryController();
            
            Assert.True (physicalController.Exists (item));
        }

        [Test]
        public void TestUploadFile(){
            string testfile = Path.Combine (RuntimeSettings.HomePath, "teste.html");
            if (!File.Exists (testfile)){               
                File.WriteAllText (testfile,"test file");
            }

            StorageQloudPhysicalRepositoryController physicalRepository = new StorageQloudPhysicalRepositoryController();
            RepositoryItem item = physicalRepository.CreateItemInstance (testfile);

            StorageQloudRemoteRepositoryController repositoryController = new StorageQloudRemoteRepositoryController();

            repositoryController.Upload (item);

            Assert.True (repositoryController.Exists (item));
        }

        [Test]
        public void TestUploadFileWhenCopy(){
            string testfile = Path.Combine (RuntimeSettings.HomePath, "teste.html");
            if (!File.Exists (testfile)){               
                File.WriteAllText (testfile,"test file");
            }

            string testcopy = Path.Combine (RuntimeSettings.HomePath, "copy.html");
            if (!File.Exists (testcopy)){               
                File.WriteAllText (testcopy,"test file");
            }



            StorageQloudPhysicalRepositoryController physicalRepository = new StorageQloudPhysicalRepositoryController();
            RepositoryItem item = physicalRepository.CreateItemInstance (testfile);
            RepositoryItem itemCopy = physicalRepository.CreateItemInstance (testcopy);

            
            StorageQloudRemoteRepositoryController repositoryController = new StorageQloudRemoteRepositoryController();

            itemCopy.LocalMD5Hash = physicalRepository.CalculateMD5Hash(itemCopy);

            repositoryController.Upload (item);
            repositoryController.Upload (itemCopy);
            
            Assert.True (repositoryController.Exists (item));
            Assert.True (repositoryController.Exists (itemCopy));
        }

        [Test]
        public void TestUploadFolder(){
            string testfile = Path.Combine (RuntimeSettings.HomePath, "Teste/");
            if (!Directory.Exists (testfile)){               
                Directory.CreateDirectory (testfile);
            }
            
            StorageQloudPhysicalRepositoryController physicalRepository = new StorageQloudPhysicalRepositoryController();
            RepositoryItem item = physicalRepository.CreateItemInstance (testfile);
            item.IsAFolder = true;
            StorageQloudRemoteRepositoryController repositoryController = new StorageQloudRemoteRepositoryController();
            
            repositoryController.Upload (item);
            
            Assert.True (repositoryController.Exists (item));
        }

        [Test]
        public void TestMoveFileToTrash(){
            string testfile = Path.Combine (RuntimeSettings.HomePath, "teste.html");
            if (!File.Exists (testfile)){               
                File.WriteAllText (testfile,"test file");
            }
            
            StorageQloudPhysicalRepositoryController physicalRepository = new StorageQloudPhysicalRepositoryController();
            RepositoryItem item = physicalRepository.CreateItemInstance (testfile);
            item.IsAFolder = false;
            StorageQloudRemoteRepositoryController repositoryController = new StorageQloudRemoteRepositoryController();

            repositoryController.Upload (item);
            repositoryController.MoveToTrash (item);
            
            Assert.False (repositoryController.Exists (item));
            Assert.True (repositoryController.ExistsVersion(item));
        }

        [Test]
        public void TestMoveEmptyFolderToTrash(){
            string testfile = Path.Combine (RuntimeSettings.HomePath, "Teste/");
            if (!Directory.Exists (testfile)){               
                Directory.CreateDirectory (testfile);
            }
            
            StorageQloudPhysicalRepositoryController physicalRepository = new StorageQloudPhysicalRepositoryController();
            RepositoryItem item = physicalRepository.CreateItemInstance (testfile);
            item.IsAFolder = true;
            StorageQloudRemoteRepositoryController repositoryController = new StorageQloudRemoteRepositoryController();
            
            repositoryController.Upload (item);
            repositoryController.MoveToTrash (item);
            
            Assert.False (repositoryController.Exists (item));
            Assert.True (repositoryController.ExistsVersion(item));
        }

        [Test]
        public void TestMoveNotEmptyFolderToTrash(){
            string testfolder = Path.Combine (RuntimeSettings.HomePath, "NotEmpty/");
            if (!Directory.Exists (testfolder)){               
                Directory.CreateDirectory (testfolder);
            }

            string testfile = Path.Combine (RuntimeSettings.HomePath, "NotEmpty", "teste.html");
            if (!File.Exists (testfile)){               
                File.WriteAllText (testfile,"test file");
            }

            StorageQloudPhysicalRepositoryController physicalRepository = new StorageQloudPhysicalRepositoryController();
            RepositoryItem item = physicalRepository.CreateItemInstance (testfolder);
            item.IsAFolder = true;
            StorageQloudRemoteRepositoryController repositoryController = new StorageQloudRemoteRepositoryController();
            
            repositoryController.Upload (item);

            RepositoryItem item2 = physicalRepository.CreateItemInstance (testfile);
            item2.IsAFolder = false;
            repositoryController.Upload (item2);



            repositoryController.MoveToTrash (item);
            
            Assert.False (repositoryController.Exists (item));
            Assert.True (repositoryController.ExistsVersion(item));
        }

        [Test]
        public void TestSendLocalVersionFileToTrash(){
            string testfile = Path.Combine (RuntimeSettings.HomePath, "local.html");
            if (!File.Exists (testfile)){               
                File.WriteAllText (testfile,"test file");
            }
            
            StorageQloudPhysicalRepositoryController physicalRepository = new StorageQloudPhysicalRepositoryController();
            RepositoryItem item = physicalRepository.CreateItemInstance (testfile);
            item.IsAFolder = false;
            StorageQloudRemoteRepositoryController repositoryController = new StorageQloudRemoteRepositoryController();

            repositoryController.SendLocalVersionToTrash (item);

            Assert.True (repositoryController.ExistsVersion(item));

        }

        [Test]
        public void TestSendLocalVersionFolderToTrash(){
            string testfile = Path.Combine (RuntimeSettings.HomePath, "Version/");
            if (!Directory.Exists (testfile)){               
                Directory.CreateDirectory (testfile);
            }
            
            StorageQloudPhysicalRepositoryController physicalRepository = new StorageQloudPhysicalRepositoryController();
            RepositoryItem item = physicalRepository.CreateItemInstance (testfile);
            item.IsAFolder = true;
            StorageQloudRemoteRepositoryController repositoryController = new StorageQloudRemoteRepositoryController();
            
            repositoryController.SendLocalVersionToTrash (item);
            
            Assert.True (repositoryController.ExistsVersion(item));            
        }

     }
}

