using System;
using NUnit.Framework;
using QloudSync.Synchrony;
using System.IO;
using QloudSync.Repository;
using System.Linq;

namespace QloudSync
{
    [TestFixture()]
    public class TestUploadSynchronizer : UploadSynchronizer
    {
        string filepath = Path.Combine (RuntimeSettings.HomePath, "file.txt");
        string folderpath = Path.Combine (RuntimeSettings.HomePath, "folder");
        [Test()]
        public void TestCreateFile ()
        {
            Init();
            LocalFile f = new LocalFile(filepath);
            CreateFile (f);
            Assert.True (remoteRepo.Files.Where(rf=> rf.MD5Hash==f.MD5Hash).Any());
        }

        [Test()]
        public void TestCreateFolder(){
            Init ();
            Folder f = new Folder(folderpath);
            CreateFile(f);
            Assert.True (remoteRepo.Files.Where (rf=> rf.AbsolutePath == f.AbsolutePath).Any());
        }

        [Test()]
        public void TestRenameFile (){
            Init ();
            LocalFile f = new LocalFile(filepath);
            BacklogSynchronizer.GetInstance().AddFile(f);
            remoteRepo.Upload (f);

            string newpath = Path.Combine(RuntimeSettings.HomePath, "newfile.txt");
            System.IO.File.Copy (filepath, newpath);
            System.IO.File.Delete (filepath);
            LocalFile nf = new LocalFile(newpath);

            CreateFile (nf);
            Assert.True (remoteRepo.Files.Where(rf=>rf.AbsolutePath==nf.AbsolutePath).Any());
            Assert.True (remoteRepo.TrashFiles.Where(tf => tf.AbsolutePath==string.Format("{0}(0)",f.AbsolutePath)).Any());
        }

        [Test ()]
        public void TestDeleteFile(){
            Init ();
            LocalFile f = new LocalFile(filepath);
            BacklogSynchronizer.GetInstance().AddFile(f);
            remoteRepo.Upload (f);
            DeleteFile (f);
            Assert.False (remoteRepo.Files.Where(rf=>rf.MD5Hash==f.MD5Hash).Any());
            Assert.True (remoteRepo.TrashFiles.Where (rf=> rf.MD5Hash==f.MD5Hash).Any());
        }

        [Test ()]
        public void TestDeleteFolder(){
            Init ();
            LocalFile f = new LocalFile(folderpath);
            BacklogSynchronizer.GetInstance().AddFile(f);
            remoteRepo.CreateFolder (new Folder(f.AbsolutePath));
            DeleteFile (f);
            Assert.False (remoteRepo.Files.Where(rf=>rf.AbsolutePath==f.AbsolutePath).Any());
            Assert.True (remoteRepo.TrashFiles.Where (rf=> rf.AbsolutePath==f.AbsolutePath).Any());
        }
        
        [Test ()]
        public void TestUpdateFile(){
            Init ();
            LocalFile f = new LocalFile(filepath);
            BacklogSynchronizer.GetInstance().AddFile(f);
            remoteRepo.Upload (f);
            System.IO.File.WriteAllText(filepath, "A original text was modified");
            LocalFile nf = new LocalFile(filepath);
            UpdateFile (f);
            Assert.True (remoteRepo.Files.Where(rf=>rf.MD5Hash==nf.MD5Hash).Any());
            Assert.True (remoteRepo.TrashFiles.Where (rf=> rf.MD5Hash==f.MD5Hash).Any());
        }

        public void Init(){
            new Test().ClearRepositories();
            System.IO.File.WriteAllText(filepath, "this a test file");
            Directory.CreateDirectory (folderpath);
        }

       
    }
}

