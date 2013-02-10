using System;
using NUnit.Framework;
using GreenQloud.Repository;
using System.IO;
using System.Linq;
using System.Threading;

namespace GreenQloud
{
    [TestFixture()]
    public class TestUploadSynchronizer
    {
        string filepath = Path.Combine (RuntimeSettings.HomePath, "file.txt");
        string folderpath = Path.Combine (RuntimeSettings.HomePath, "folder/");
        RemoteRepo remoteRepo = new RemoteRepo();

        [Test()]
        public void TestCreateFile ()
        {
            if (!Directory.Exists(RuntimeSettings.HomePath))
                Directory.CreateDirectory (RuntimeSettings.HomePath);
            if(System.IO.File.Exists(filepath))
                System.IO.File.Delete(filepath);
            UploadController.GetInstance();

            System.IO.File.WriteAllText (filepath, "texto");            
            LocalFile f = new LocalFile(filepath);
            DateTime init = DateTime.Now;
            while (DateTime.Now.Subtract(init).TotalSeconds<20);
            Assert.True (remoteRepo.Files.Any(rf=> rf.MD5Hash==f.MD5Hash && rf.AbsolutePath == f.AbsolutePath));
        }
        
       [Test()]
        public void TestCreateFolder ()
        {

            if (!Directory.Exists (RuntimeSettings.HomePath))
                Directory.CreateDirectory (RuntimeSettings.HomePath);
            if (Directory.Exists (folderpath))
                Directory.Delete (folderpath);
                
            UploadController.GetInstance ();
            Directory.CreateDirectory (folderpath);

            Folder f = new Folder (string.Format ("{0}/", folderpath));
            DateTime init = DateTime.Now;
            while (DateTime.Now.Subtract(init).TotalSeconds<30);

            Assert.True (remoteRepo.Files.Any (rf=> rf.AbsolutePath == f.AbsolutePath));
        }
        
        [Test()]
        public void TestRenameFile (){
            if (!Directory.Exists(RuntimeSettings.HomePath))
                Directory.CreateDirectory (RuntimeSettings.HomePath);
            if(!System.IO.File.Exists(filepath))
                System.IO.File.WriteAllText (filepath, "texto");  
            LocalFile f = new LocalFile(filepath);
            remoteRepo.Upload (f);
            string newpath = Path.Combine(RuntimeSettings.HomePath, "newfile.txt");
            if (System.IO.File.Exists(newpath))
                System.IO.File.Delete (newpath);
            UploadController.GetInstance();
            System.IO.File.Move (filepath, newpath);
            LocalFile nf = new LocalFile(newpath);
            DateTime init = DateTime.Now;
            while (DateTime.Now.Subtract(init).TotalSeconds<20);

            Assert.True (remoteRepo.Files.Any(rf=>rf.AbsolutePath==nf.AbsolutePath));
            Assert.True (remoteRepo.TrashFiles.Any(tf => tf.AbsolutePath==string.Format("{0}(1)",f.AbsolutePath)));
        }
        
        [Test ()]
        public void TestDeleteFile(){

            if (!Directory.Exists(RuntimeSettings.HomePath))
                Directory.CreateDirectory (RuntimeSettings.HomePath);
            if(!System.IO.File.Exists(filepath))
                System.IO.File.WriteAllText (filepath, "texto");  

            LocalFile f = new LocalFile(filepath);
            remoteRepo.Upload (f);
            UploadController.GetInstance();
            System.IO.File.Delete (filepath);
            DateTime init = DateTime.Now;
            while (DateTime.Now.Subtract(init).TotalSeconds<20);
            Assert.False (remoteRepo.Files.Any(rf=>rf.MD5Hash==f.MD5Hash && rf.AbsolutePath==f.AbsolutePath));
            Assert.True (remoteRepo.TrashFiles.Any (rf=> rf.MD5Hash==f.MD5Hash));
        }
        
        [Test ()]
        public void TestDeleteFolder(){
            if (!Directory.Exists(RuntimeSettings.HomePath))
                Directory.CreateDirectory (RuntimeSettings.HomePath);
            if (!Directory.Exists(folderpath))
                Directory.CreateDirectory (folderpath);
            Folder f = new Folder(folderpath);
            remoteRepo.CreateFolder (f);
            UploadController.GetInstance();
            Directory.Delete(folderpath);
            DateTime init = DateTime.Now;
            while (DateTime.Now.Subtract(init).TotalSeconds<20);
            Assert.False (remoteRepo.Files.Where(rf=>rf.AbsolutePath==f.AbsolutePath).Any());
            Assert.True (remoteRepo.TrashFiles.Where (rf=> rf.AbsolutePath==f.AbsolutePath).Any());
        }
        
        [Test ()]
        public void TestUpdateFile(){

            if (!Directory.Exists(RuntimeSettings.HomePath))
                Directory.CreateDirectory (RuntimeSettings.HomePath);
            if(!System.IO.File.Exists(filepath))
                System.IO.File.WriteAllText (filepath, "texto"); 

            LocalFile f = new LocalFile(filepath);
            remoteRepo.Upload (f);
            UploadController.GetInstance();
            System.IO.File.WriteAllText(filepath, "A original text was modified");
            LocalFile nf = new LocalFile(filepath);
            DateTime init = DateTime.Now;
            while (DateTime.Now.Subtract(init).TotalSeconds<20);
            Assert.True (remoteRepo.Files.Where(rf=>rf.MD5Hash==nf.MD5Hash).Any());
            Assert.True (remoteRepo.TrashFiles.Where (rf=> rf.MD5Hash==f.MD5Hash).Any());
        }
        
      

    }
}

