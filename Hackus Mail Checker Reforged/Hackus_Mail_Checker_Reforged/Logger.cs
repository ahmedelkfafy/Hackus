using System;
using System.IO;

namespace Hackus_Mail_Checker_Reforged
{
	// Token: 0x0200001F RID: 31
	internal static class Logger
	{
		// Token: 0x060000AA RID: 170 RVA: 0x000119D4 File Offset: 0x0000FBD4
		public static void LogError(Exception exception, string output = "HackusErrors.txt")
		{
			object locker = Logger._locker;
			lock (locker)
			{
				try
				{
					using (StreamWriter streamWriter = new StreamWriter(output, true))
					{
						streamWriter.WriteLine("START: ");
						streamWriter.WriteLine("Message: " + exception.Message);
						streamWriter.WriteLine("StackTrace: " + exception.StackTrace);
						streamWriter.WriteLine("Source: " + exception.Source);
						streamWriter.WriteLine("END: ");
					}
				}
				catch
				{
				}
			}
		}

		// Token: 0x0400005F RID: 95
		private static object _locker = new object();
	}
}
