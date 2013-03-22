using System;

 namespace GreenQloud.Model
{
	
	public class Event
	{
		public Event ()
		{
		}

        public Event (RepositoryItem file, System.IO.WatcherChangeTypes changeEvent)
		{
			File = file;
            EventType = changeEvent;
		}

        public RepositoryItem File{
			set; get;
		}

        public System.IO.WatcherChangeTypes EventType{
			set; get;
		}

		public string User {
			set; get;
		}
	}
}

