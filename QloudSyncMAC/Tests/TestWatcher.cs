using System;
using NUnit.Framework;
using System.IO;
using GreenQloud.Repository;
using System.Linq;

namespace GreenQloud
{
    [TestFixture()]
    public class TestWatcher : Test
    {
//
//
//        [Test()]
//        public void TestCreateFile ()
//        {
//            this.ClearRepositories();
//            //base.w.CreateWatcher (RuntimeSettings.HomePath);
//            string path = Path.Combine (RuntimeSettings.HomePath, "file.txt");
//            System.IO.File.WriteAllText(path, "Simple file test");
//            GreenQloud.Repository.StorageQloudObject file = new GreenQloud.Repository.StorageQloudObject (path);
//
//            DateTime reff = DateTime.Now;
//            while (DateTime.Now.Subtract(reff).TotalSeconds<4);
//            while (UploadController.GetInstance().Status == SyncStatus.Sync);
//
//            Assert.True (remoteRepo.Files.Count == 1);
//            Assert.True (this.remoteRepo.Files.Any (rf => rf.MD5Hash == file.MD5Hash && rf.AbsolutePath == file.AbsolutePath));
//
//        }
//
//        [Test()]
//        public void TestCreateFolder()
//        {
//            this.ClearRepositories();
//          //  base.w.CreateWatcher (RuntimeSettings.HomePath);
//
//            Directory.CreateDirectory (Path.Combine (RuntimeSettings.HomePath, "folder"));
//            Folder folder = new Folder(Path.Combine (RuntimeSettings.HomePath, "folder"));
//            DateTime reff = DateTime.Now;
//            while (DateTime.Now.Subtract(reff).TotalSeconds<4);
//            while (UploadController.GetInstance().Status == SyncStatus.Sync);
//
//           // Assert.True (w.watchers.Count == 2);
//            Assert.True (remoteRepo.Files.Count==1);
//            Assert.True (remoteRepo.Files.Any (rf =>rf.AbsolutePath == string.Format("{0}/",folder.AbsolutePath)));
//        }
//
//        [Test()]
//        public void TestUpdateFile()
//        {
//        }
//
//        [Test()]
//        public void TestMoveFile ()
//        {
//            this.ClearRepositories ();
//          //  base.w.CreateWatcher (RuntimeSettings.HomePath);
//
//            string path = Path.Combine (RuntimeSettings.HomePath, "file.txt");
//            string newpath = Path.Combine (RuntimeSettings.HomePath, "move/file.txt");
//            Directory.CreateDirectory (Path.Combine(RuntimeSettings.HomePath,"move"));
//            System.IO.File.WriteAllText (path, "simple test file");
//            StorageQloudObject old = new StorageQloudObject (path);
//            System.IO.File.Move (path, newpath);
//            StorageQloudObject file = new StorageQloudObject (newpath);
//            DateTime reff = DateTime.Now;
//            while (DateTime.Now.Subtract(reff).TotalSeconds<30);
//            while (UploadController.GetInstance().Status == SyncStatus.Sync);
//
//            Assert.True (remoteRepo.Files.Any (rf=> rf.MD5Hash == file.MD5Hash && rf.AbsolutePath == file.AbsolutePath));
//            Assert.False (remoteRepo.Files.Any (rf=> rf.MD5Hash == old.MD5Hash && rf.AbsolutePath == old.AbsolutePath));
//
//        }
//
//        [Test()]
//        public void TestMoveEmptyFolder(){
//            this.ClearRepositories ();
//        //    base.w.CreateWatcher (RuntimeSettings.HomePath);
//            
//            string old = Path.Combine(RuntimeSettings.HomePath,"origin");
//            string newpath = Path.Combine (RuntimeSettings.HomePath, "move");
//            string path = "folder/";
//            Directory.CreateDirectory (old);
//            Directory.CreateDirectory(newpath);
//            string newfullpath = Path.Combine(newpath, path);
//            string oldfullpath = Path.Combine(old, path);
//            Directory.CreateDirectory (oldfullpath);
//            Console.WriteLine (oldfullpath);
//
//            Console.WriteLine (newfullpath);
//            System.IO.Directory.Move (oldfullpath, newfullpath);
//
//            DateTime reff = DateTime.Now;
//            while (DateTime.Now.Subtract(reff).TotalSeconds<30);
//            while (UploadController.GetInstance().Status == SyncStatus.Sync);
//            
//            Assert.True (remoteRepo.Files.Any (rf=> rf.AbsolutePath == new StorageQloudObject(newfullpath).AbsolutePath));
//            Assert.False (remoteRepo.Files.Any (rf=> rf.AbsolutePath == new StorageQloudObject(oldfullpath).AbsolutePath));
//        }
//
//
//        [Test()]
//        public void TestMoveFullFolder(){
//            this.ClearRepositories ();
//            
//          //  base.w.CreateWatcher (RuntimeSettings.HomePath);
//            
//            string old = Path.Combine(RuntimeSettings.HomePath,"origin");
//            string newpath = Path.Combine (RuntimeSettings.HomePath, "move");
//            string path = Path.Combine (old, "folder");
//            string npath = Path.Combine(newpath, "folder");
//            Directory.CreateDirectory (old);
//            Directory.CreateDirectory (newpath);
//            Directory.CreateDirectory (path);
//            for (int i = 1 ; i <11; i++)
//                System.IO.File.WriteAllText (Path.Combine(path, string.Format("teste{0}.txt", i)), string.Format("{0}{1}",i,"simple test file"));
//            
//
//            System.IO.Directory.Move (path, npath);
//            
//            DateTime reff = DateTime.Now;
//            while (DateTime.Now.Subtract(reff).TotalSeconds<60);
//            while (UploadController.GetInstance().Status == SyncStatus.Sync);
//            Assert.True (remoteRepo.Files.Count (rf=> rf.AbsolutePath.Contains (new StorageQloudObject(path).AbsolutePath))==0);
//            Assert.True (remoteRepo.Files.Count (rf=> rf.AbsolutePath.Contains(new StorageQloudObject(npath).AbsolutePath))==10);
//            
//        }
//
//        [Test()]
//        public void TestRenameFolder(){
//            this.ClearRepositories ();
//            
//          //  base.w.CreateWatcher (RuntimeSettings.HomePath);
//            
//            string old = Path.Combine(RuntimeSettings.HomePath,"origin");
//            string newpath = Path.Combine (RuntimeSettings.HomePath, "move");
//            
//            Directory.CreateDirectory (old);
//            
//            for (int i = 1 ; i <11; i++)
//                System.IO.File.WriteAllText (Path.Combine(old, string.Format("teste{0}.txt", i)), string.Format("{0}{1}",i,"simple test file"));
//            
//            
//            System.IO.Directory.Move (old, newpath);
//            
//            DateTime reff = DateTime.Now;
//            while (DateTime.Now.Subtract(reff).TotalSeconds<60);
//            while (UploadController.GetInstance().Status == SyncStatus.Sync);
//            Assert.True (remoteRepo.Files.Count (rf=> rf.AbsolutePath.Contains (new StorageQloudObject(old).AbsolutePath))==0);
//            Assert.True (remoteRepo.Files.Count (rf=> rf.AbsolutePath.Contains(new StorageQloudObject(newpath).AbsolutePath))==10);
//            
//        }
//        
//        [Test()]
//        public void TestRenameFile(){
//            this.ClearRepositories ();
//            
//        //    base.w.CreateWatcher (RuntimeSettings.HomePath);
//            
//            string old = Path.Combine(RuntimeSettings.HomePath,"origin");
//            string newpath = Path.Combine (RuntimeSettings.HomePath, "move");
//            
//            Directory.CreateDirectory (old);            
//            Directory.CreateDirectory (newpath);
//
//            System.IO.File.WriteAllText (Path.Combine(old, "teste.txt"), "simple test file");
//            StorageQloudObject newfile = new StorageQloudObject(Path.Combine(newpath, "teste.txt"));
//            StorageQloudObject oldfile = new StorageQloudObject (Path.Combine(old, "teste.txt"));
//            System.IO.File.Move (oldfile.FullLocalName, newfile.FullLocalName);
//            
//            DateTime reff = DateTime.Now;
//            while (DateTime.Now.Subtract(reff).TotalSeconds<30);
//            while (UploadController.GetInstance().Status == SyncStatus.Sync);
//            Assert.True (remoteRepo.Files.Any (rf=> rf.AbsolutePath== newfile.AbsolutePath));
//            Assert.False (remoteRepo.Files.Any (rf=> rf.AbsolutePath==oldfile.AbsolutePath));
//            
//        }
        

    }
}

