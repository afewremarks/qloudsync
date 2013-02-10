using System;
using NUnit.Framework;
using GreenQloud.Synchrony;
using System.IO;
using System.Xml;
using GreenQloud.Repository;
using System.Threading;
using System.Linq;

namespace GreenQloud
{
    [TestFixture()]
    public class TestBacklog
    {
        RemoteRepo remoteRepo = new RemoteRepo();
        Test util = new Test();
        [Test ()]
        public void TestBacklogFileCreate ()
        {
            System.IO.File.Delete (RuntimeSettings.BacklogFile);
            BacklogSynchronizer.GetInstance().Create();
            Assert.True(System.IO.File.Exists(RuntimeSettings.BacklogFile));
        }

        [Test ()]
        public void TestBacklogFileWasCreateWithPredefinedStructure ()
        {
            System.IO.File.Delete (RuntimeSettings.BacklogFile);
            BacklogSynchronizer.GetInstance().Create();
            XmlNode root_node = BacklogSynchronizer.GetInstance().ChildNodes[1];

            Assert.True (root_node.Name == "files");
        }

        [Test ()]
        public void TestSynchronizeDeleteLocalFileOffline(){

            util.ClearRepositories();

            string path = Path.Combine(RuntimeSettings.HomePath, "Testfile.txt");
            //criar um arquivo
            System.IO.File.WriteAllText (path, "Test synchronize");
            //adicionar um arquivo no backlog
            LocalFile file = new LocalFile(path);

            BacklogSynchronizer.GetInstance().AddFile (file);
            //fazer upload
            remoteRepo.Upload (file);
            //apagar ele
            System.IO.File.Delete (file.FullLocalName);
            //sincronizar
            BacklogSynchronizer.GetInstance().Synchronize();
            //arquivo remoto deve existir apenas no trash
            Assert.False (remoteRepo.ExistsInBucket(file));
        }

        [Test ()]
        public void TestSynchronizeCreateLocalFileOffline(){
            util.ClearRepositories();

            
            string path = Path.Combine(RuntimeSettings.HomePath, "Testfile.txt");
            //criar um arquivo
            System.IO.File.WriteAllText (path, "Test synchronize");
            //adicionar um arquivo no backlog
            LocalFile file = new LocalFile(path);
            
            BacklogSynchronizer.GetInstance().AddFile (file);
            //fazer upload
            remoteRepo.Upload (file);
            //criar novo
            string newpath = Path.Combine(RuntimeSettings.HomePath, "Testcreate.txt");
            System.IO.File.WriteAllText (newpath, "Test add synchronize");
            LocalFile newfile = new LocalFile(path);
            //sincronizar
            BacklogSynchronizer.GetInstance().Synchronize();
            //arquivo remoto deve existir
            Assert.True(remoteRepo.ExistsInBucket(newfile));
        }

        [Test ()]
        public void TestSynchronizeChangeLocalFileOffline(){

            util.ClearRepositories();

            string path = Path.Combine(RuntimeSettings.HomePath, "Testfile.txt");
            //criar um arquivo
            System.IO.File.WriteAllText (path, "Test synchronize");
            //adicionar um arquivo no backlog
            LocalFile file = new LocalFile(path);
            
            BacklogSynchronizer.GetInstance().AddFile (file);
            //fazer upload
            remoteRepo.Upload (file);
            //editar
            System.IO.File.WriteAllText (path, "Test editing synchronize");
            LocalFile file2 = new LocalFile(path);
            //sincronizar
            BacklogSynchronizer.GetInstance().Synchronize();
            //tem que existir o hash no servidor
            Assert.True(remoteRepo.Files.Any(rf=> rf.MD5Hash == file2.MD5Hash));
        }

        [Test ()]
        public void TestSynchronizeCreateRemoteFileOffline(){
            util.ClearRepositories();


            string path = Path.Combine(RuntimeSettings.HomePath, "Testfile.txt");
            //criar um arquivo
            System.IO.File.WriteAllText (path, "Test synchronize");
            //adicionar um arquivo no backlog
            LocalFile file = new LocalFile(path);
            //fazer upload
            remoteRepo.Upload (file);
            System.IO.File.Delete (file.FullLocalName);
               
            //sincronizar

            BacklogSynchronizer.GetInstance().Synchronize();
            //arquivo remoto deve existir
            Assert.True(System.IO.File.Exists(file.FullLocalName));

        }

        [Test()]
        public void TestSynchronizeDeleteRemoteFileOffline(){
            util.ClearRepositories();
                        
            string path = Path.Combine(RuntimeSettings.HomePath, "Testfile.txt");
            //criar um arquivo
            System.IO.File.WriteAllText (path, "Test synchronize");
            //adicionar um arquivo no backlog
            LocalFile file = new LocalFile(path);
            BacklogSynchronizer.GetInstance().AddFile (file);

            BacklogSynchronizer.GetInstance().Synchronize();
            //arquivo remoto deve existir
            Assert.False(System.IO.File.Exists(file.FullLocalName));

        }

        [Test()]
        public void TestSynchronizeCreateLocalFolderOffline ()
        {
            util.ClearRepositories();
            string path = Path.Combine(RuntimeSettings.HomePath, "TestFolder/");
            System.IO.Directory.CreateDirectory (path);
            BacklogSynchronizer.GetInstance().Synchronize();
            Assert.True (remoteRepo.Files.Any (rf => rf.AbsolutePath == new Folder(path).AbsolutePath));
        }

        [Test()]
        public void TestSynchronizeCreateRemoteFolderOffline ()
        {
            util.ClearRepositories();
            string path = Path.Combine(RuntimeSettings.HomePath, "TestFolder/");
            Folder f = new Folder (path);
            remoteRepo.CreateFolder(f);
            BacklogSynchronizer.GetInstance().Synchronize();
            Assert.True (Directory.Exists(path));
        }

        [Test()]
        public void TestSynchronizeDeleteLocalFolderOffline ()
        {
            util.ClearRepositories();
            string path = Path.Combine(RuntimeSettings.HomePath, "TestFolder/");
            Folder f = new Folder (path);
            remoteRepo.CreateFolder(f);
            BacklogSynchronizer.GetInstance().AddFile(f);
            BacklogSynchronizer.GetInstance().Synchronize();
            Assert.False (remoteRepo.Files.Any (rf => rf.AbsolutePath == new Folder(path).AbsolutePath));
        }

        [Test()]
        public void TestSynchronizeDeleteRemoteFolderOffline ()
        {
            util.ClearRepositories();
            string path = Path.Combine(RuntimeSettings.HomePath, "TestFolder/");
            Folder f = new Folder (path);
            Directory.CreateDirectory (path);
            BacklogSynchronizer.GetInstance().AddFile(f);
            BacklogSynchronizer.GetInstance().Synchronize();
            Assert.False (Directory.Exists(path));
        }


        public void TestAddFile(){
        }

        public void TestRemoveFileById(){
        }

        public void TestRemoveFileByAbsolutePath ()
        {
        }
        
        public void TestRemoveFileByHash ()
        {

        }
        
        public void TestEditFileById (){

        }
        
        public void TestEditFileByHash ()
        {

        }
        
        public void TestEditFileByName()
        {

        }
        
        protected void TestUpdateFile()
        {

        }
        
        protected void TestGet ()
        {
         
        }


    }
}

