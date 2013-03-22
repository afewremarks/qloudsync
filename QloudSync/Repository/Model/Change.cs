using System;

 namespace GreenQloud.Repository.Model
{
	
	public class Change
	{
		public Change ()
		{
		}

        public Change (RepoObject file, System.IO.WatcherChangeTypes changeEvent)
		{
			File = file;
            Event = changeEvent;
		}

        public RepoObject File{
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

