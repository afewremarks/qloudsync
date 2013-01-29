using System;
using NUnit.Framework;
using System.IO;
using QloudSync.Repository;
using System.Linq;

namespace QloudSync
{
    [TestFixture()]
    public class TestWatcher : Test
    {


        [Test()]
        public void TestCreateFile ()
        {
            this.ClearRepositories();
            base.w.CreateWatcher (RuntimeSettings.HomePath);
            string path = Path.Combine (RuntimeSettings.HomePath, "file.txt");
            System.IO.File.WriteAllText(path, "Simple file test");
            QloudSync.Repository.LocalFile file = new QloudSync.Repository.LocalFile (path);

            DateTime reff = DateTime.Now;
            while (DateTime.Now.Subtract(reff).TotalSeconds<4);
            while (UploadController.GetInstance().Status == SyncStatus.Sync);

            Assert.True (remoteRepo.Files.Count == 1);
            Assert.True (this.remoteRepo.Files.Where (rf => rf.MD5Hash == file.MD5Hash && rf.AbsolutePath == file.AbsolutePath).Any());

        }

        [Test()]
        public void TestCreateFolder()
        {
            this.ClearRepositories();
            base.w.CreateWatcher (RuntimeSettings.HomePath);

            Directory.CreateDirectory (Path.Combine (RuntimeSettings.HomePath, "folder"));
            Folder folder = new Folder(Path.Combine (RuntimeSettings.HomePath, "folder"));
            DateTime reff = DateTime.Now;
            while (DateTime.Now.Subtract(reff).TotalSeconds<4);
            while (UploadController.GetInstance().Status == SyncStatus.Sync);

            Assert.True (w.watchers.Count == 2);
            Assert.True (remoteRepo.Files.Count==1);
            Assert.True (remoteRepo.Files.Where (rf =>rf.AbsolutePath == string.Format("{0}/",folder.AbsolutePath)).Any());
        }

        [Test()]
        public void TestUpdateFile()
        {
        }

        [Test()]
        public void TestMoveFile ()
        {
        }

        [Test()]
        public void TestMoveEmptyFolder(){
        }

        [Test()]
        public void TestMoveFullFolder(){
        }
    }
}

