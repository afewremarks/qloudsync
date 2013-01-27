using System;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using System.Collections.Generic;

 namespace QloudSync.Repository
{
	public class LocalFile : File
	{
		public LocalFile (string absolutePath, DateTime timeOfLastChange) : base (absolutePath){
			SetMD5Hash();
			TimeOfLastChange = timeOfLastChange;
			Deleted = false;
		}
		public LocalFile (string absolutePath) : base (absolutePath)
		{
            SetMD5Hash();
            TimeOfLastChange = DateTime.Now;
            Deleted = false;
		}

		public override string MD5Hash {
            get ; set;
			
		}

        public void SetMD5Hash ()
		{

			if (!System.IO.File.Exists(FullLocalName)) 
				return;
			
			try {
		
				FileStream fs = System.IO.File.Open (FullLocalName, FileMode.Open);
				MD5 md5 = MD5.Create ();
				MD5Hash = BitConverter.ToString (md5.ComputeHash (fs)).Replace (@"-", @"").ToLower ();
				fs.Close ();
			} catch {
				MD5Hash = "";

			}
            
            //return hash;
        }

		public bool IsFileLocked {
			get {
				FileStream stream = null;
			
				try {
					FileInfo file = new FileInfo (FullLocalName);
					if (file.Exists)
						stream = file.Open (FileMode.Open, FileAccess.ReadWrite, FileShare.None);
					else
						return false;
				} catch (IOException) {
					return true;
				} finally {
					if (stream != null)
						stream.Close ();
				}
				return false;
			}
		}

        public bool Copying {
            get {
                FileInfo file = new FileInfo (FullLocalName);

                long sizeinit = file.Length;
                System.Threading.Thread.Sleep (500);
                long sizeend = file.Length;
                if(sizeend == 0)
                    return false;
                return sizeinit != sizeend;
            }
        }

		public new bool IsIgnoreFile {
			get {
				return base.IsIgnoreFile; //|| IsFileLocked;
			}
		}

		public static List <File> Get (List <FileInfo> fileInfos)
		{
			List <File> localFiles = new List <File> ();
			foreach (FileInfo fileInfo in fileInfos) {
				LocalFile localFile = new LocalFile (fileInfo.FullName, fileInfo.LastWriteTime);
				if(!localFile.IsIgnoreFile)
					localFiles.Add (localFile);
			}
			return localFiles;
		}
	}
}
