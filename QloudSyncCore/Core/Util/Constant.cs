using System;

 namespace GreenQloud.Util
{
	public static class Constant
	{
		public const string DEFAULT = "-default";
		public const string TRASH = ".trash";

        public const string TEMP = "/.tmp";

		public static string EMPTY = "";


		public const string DELIMITER = "/";
		public const string DELIMITER_INVERSE = @"\";


        public static string CLOCK_TIME = "clock.time";
      

		public const int KEYS_LENGTH = 40;
		public const int KEY_SECRET_START_INDEX= 21;
		public const int KEY_PUBLIC_START_INDEX = 83;
		public const string ADDRESS_TO_AUTHENTICATE = "https://manage.greenqloud.com/api/v1/account-keys/"; 

        static string app_data_path = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);
        static string config_path   = System.IO.Path.Combine (app_data_path, "qloudsync");

        public static string [] EXCLUDE_FILES = new string [] {
            "*.autosave", // Various autosaving apps
            "*~", // gedit and emacs
            ".~lock.*", // LibreOffice
            "*.part", "*.crdownload", // Firefox and Chromium temporary download files
            ".*.sw[a-z]", "*.un~", "*.swp", "*.swo", // vi(m)
            ".directory", // KDE
            ".DS_Store", "Icon\r", "._*", ".Spotlight-V100", ".Trashes", // Mac OS X
            "*(Autosaved).graffle", // Omnigraffle
            "Thumbs.db", "Desktop.ini", // Windows
            "~*.tmp", "~*.TMP", "*~*.tmp", "*~*.TMP", // MS Office
            "~*.ppt", "~*.PPT", "~*.pptx", "~*.PPTX",
            "~*.xls", "~*.XLS", "~*.xlsx", "~*.XLSX",
            "~*.doc", "~*.DOC", "~*.docx", "~*.DOCX",
            "*/CVS/*", ".cvsignore", "*/.cvsignore", // CVS
            "/.svn/*", "*/.svn/*", // Subversion
            "/.hg/*", "*/.hg/*", "*/.hgignore", // Mercurial
            "/.bzr/*", "*/.bzr/*", "*/.bzrignore", // Bazaar
            ".sparkleshare"
        };


        public static string KEY_FILE { 
            get {        

                return System.IO.Path.Combine (config_path, "keys.k"); 
            } 
        }
        
        public static string BACKLOG_FILE {
            get {        
                return System.IO.Path.Combine (config_path, "backlog.txt"); 
            }
        }
	}
}

