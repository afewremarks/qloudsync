using System;
using SQ.Synchrony;

namespace SQ.Repository
{
	public class Repo
	{

		protected Repo ()
		{

		}

		public static DateTime LastSyncTime
		{
			set;get;
		}

		public static System.Collections.Generic.List<Sync> Syncs{
			set; get;
		}


	}
}

