using System;

 namespace QloudSync.Repository
{
	
	public class Change
	{
		public Change ()
		{
		}

		public Change (File file, System.IO.WatcherChangeTypes changeEvent)
		{
			File = file;
            Event = changeEvent;
		}

		public File File{
			set; get;
		}

        public System.IO.WatcherChangeTypes Event{
			set; get;
		}

		public string User {
			set; get;
		}
	}
}

