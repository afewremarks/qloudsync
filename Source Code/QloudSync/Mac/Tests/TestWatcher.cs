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
            this.ClearRepositories ();
            base.w.CreateWatcher (RuntimeSettings.HomePath);

            string path = Path.Combine (RuntimeSettings.HomePath, "file.txt");
            string newpath = Path.Combine (RuntimeSettings.HomePath, "move/file.txt");
            Directory.CreateDirectory (Path.Combine(RuntimeSettings.HomePath,"move"));
            System.IO.File.WriteAllText (path, "simple test file");
            LocalFile old = new LocalFile (path);
            System.IO.File.Move (path, newpath);
            LocalFile file = new LocalFile (newpath);
            DateTime reff = DateTime.Now;
            while (DateTime.Now.Subtract(reff).TotalSeconds<30);
            while (UploadController.GetInstance().Status == SyncStatus.Sync);

            Assert.True (remoteRepo.Files.Where (rf=> rf.MD5Hash == file.MD5Hash && rf.AbsolutePath == file.AbsolutePath).Any());
            Assert.False (remoteRepo.Files.Where (rf=> rf.MD5Hash == old.MD5Hash && rf.AbsolutePath == old.AbsolutePath).Any());

        }

        [Test()]
        public void TestMoveEmptyFolder(){
            this.ClearRepositories ();
            base.w.CreateWatcher (RuntimeSettings.HomePath);
            
            string old = Path.Combine(RuntimeSettings.HomePath,"origin");
            string newpath = Path.Combine (RuntimeSettings.HomePath, "move");
            string path = "folder/";
            Directory.CreateDirectory (old);
            Directory.CreateDirectory(newpath);
            string newfullpath = Path.Combine(newpath, path);
            string oldfullpath = Path.Combine(old, path);
            Directory.CreateDirectory (oldfullpath);
            Console.WriteLine (oldfullpath);

            Console.WriteLine (newfullpath);
            System.IO.Directory.Move (oldfullpath, newfullpath);

            DateTime reff = DateTime.Now;
            while (DateTime.Now.Subtract(reff).TotalSeconds<30);
            while (UploadController.GetInstance().Status == SyncStatus.Sync);
            
            Assert.True (remoteRepo.Files.Where (rf=> rf.AbsolutePath == new LocalFile(newfullpath).AbsolutePath).Any());
            Assert.False (remoteRepo.Files.Where (rf=> rf.AbsolutePath == new LocalFile(oldfullpath).AbsolutePath).Any());
        }


        [Test()]
        public void TestMoveFullFolder(){
            this.ClearRepositories ();
            
            base.w.CreateWatcher (RuntimeSettings.HomePath);
            
            string old = Path.Combine(RuntimeSettings.HomePath,"origin");
            string newpath = Path.Combine (RuntimeSettings.HomePath, "move");
            string path = Path.Combine (old, "folder");
            string npath = Path.Combine(newpath, "folder");
            Directory.CreateDirectory (old);
            Directory.CreateDirectory (newpath);
            Directory.CreateDirectory (path);
            for (int i = 1 ; i <11; i++)
                System.IO.File.WriteAllText (Path.Combine(path, string.Format("teste{0}.txt", i)), string.Format("{0}{1}",i,"simple test file"));
            

            System.IO.Directory.Move (path, npath);
            
            DateTime reff = DateTime.Now;
            while (DateTime.Now.Subtract(reff).TotalSeconds<60);
            while (UploadController.GetInstance().Status == SyncStatus.Sync);
            Assert.True (remoteRepo.Files.Count (rf=> rf.AbsolutePath.Contains (new LocalFile(path).AbsolutePath))==0);
            Assert.True (remoteRepo.Files.Count (rf=> rf.AbsolutePath.Contains(new LocalFile(npath).AbsolutePath))==10);
            
        }

        [Test()]
        public void TestRenameFolder(){
            this.ClearRepositories ();
            
            base.w.CreateWatcher (RuntimeSettings.HomePath);
            
            string old = Path.Combine(RuntimeSettings.HomePath,"origin");
            string newpath = Path.Combine (RuntimeSettings.HomePath, "move");
            
            Directory.CreateDirectory (old);
            
            for (int i = 1 ; i <11; i++)
                System.IO.File.WriteAllText (Path.Combine(old, string.Format("teste{0}.txt", i)), string.Format("{0}{1}",i,"simple test file"));
            
            
            System.IO.Directory.Move (old, newpath);
            
            DateTime reff = DateTime.Now;
            while (DateTime.Now.Subtract(reff).TotalSeconds<60);
            while (UploadController.GetInstance().Status == SyncStatus.Sync);
            Assert.True (remoteRepo.Files.Count (rf=> rf.AbsolutePath.Contains (new LocalFile(old).AbsolutePath))==0);
            Assert.True (remoteRepo.Files.Count (rf=> rf.AbsolutePath.Contains(new LocalFile(newpath).AbsolutePath))==10);
            
        }
        
        [Test()]
        public void TestRenameFile(){
            this.ClearRepositories ();
            
            base.w.CreateWatcher (RuntimeSettings.HomePath);
            
            string old = Path.Combine(RuntimeSettings.HomePath,"origin");
            string newpath = Path.Combine (RuntimeSettings.HomePath, "move");
            
            Directory.CreateDirectory (old);            
            Directory.CreateDirectory (newpath);

            System.IO.File.WriteAllText (Path.Combine(old, "teste.txt"), "simple test file");
            LocalFile newfile = new LocalFile(Path.Combine(newpath, "teste.txt"));
            LocalFile oldfile = new LocalFile (Path.Combine(old, "teste.txt"));
            System.IO.File.Move (oldfile.FullLocalName, newfile.FullLocalName);
            
            DateTime reff = DateTime.Now;
            while (DateTime.Now.Subtract(reff).TotalSeconds<30);
            while (UploadController.GetInstance().Status == SyncStatus.Sync);
            Assert.True (remoteRepo.Files.Where (rf=> rf.AbsolutePath== newfile.AbsolutePath).Any());
            Assert.False (remoteRepo.Files.Where (rf=> rf.AbsolutePath==oldfile.AbsolutePath).Any());
            
        }
        

    }
}

