using Amazon.S3.Model;

using System;
using System.Collections.Generic;
using System.Linq;
using QloudSync.Util;



namespace  QloudSync.Repository
{
	public class RemoteFile : File
	{
		private S3Object s3Obj;

		public RemoteFile (string absolutePath) : base (absolutePath)
		{
			this.s3Obj = new S3Object();
		}

		public RemoteFile (S3Object s3Object) : base (s3Object.Key)
		{
			this.s3Obj = s3Object;
		}


		#region implemented abstract members of File
		public override string MD5Hash {
            set{}
			get {
				return s3Obj.ETag.Replace("\"","");
			}
		}

		#endregion

		public new bool IsAFolder {
			get {
				return Name.EndsWith (Constant.DELIMITER) || Name.EndsWith (Constant.DELIMITER_INVERSE);
			}
		}

		public S3Object AsS3Object {
			get {
				return s3Obj;
			}
		}

		/*public static List<RemoteFile>  Get (System.Collections.Generic.List<S3Object> files)
        {
            List <RemoteFile> remoteFiles = new List <RemoteFile> ();

            foreach (S3Object file in files) 
                remoteFiles.Add (new RemoteFile (file));

			return remoteFiles;
		}*/
	}
}

