using System;

 namespace GreenQloud.Repository
{
	
	public class Change
	{
		public Change ()
		{
		}

        public Change (StorageQloudObject file, System.IO.WatcherChangeTypes changeEvent)
		{
			File = file;
            Event = changeEvent;
		}

        public StorageQloudObject File{
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

