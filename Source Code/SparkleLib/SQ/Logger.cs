using System;

namespace SQ.Util
{
	public class Logger
	{
		public static void LogInfo (string type, string message)
		{
			string timestamp = DateTime.Now.ToString ("HH:mm:ss");
			string line      = timestamp + " | " + type + " | " + message;
			
			Console.WriteLine (line);
		}

		public static void LogInfo (string type, Exception e)
		{
			string message = e.GetType()+"\n"+e.Message+"\n"+e.StackTrace;
			LogInfo (type, message);
		}
	}
}

