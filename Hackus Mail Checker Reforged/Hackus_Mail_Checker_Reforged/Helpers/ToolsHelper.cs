using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Hackus_Mail_Checker_Reforged.Helpers
{
	// Token: 0x0200015C RID: 348
	internal static class ToolsHelper
	{
		// Token: 0x06000A25 RID: 2597 RVA: 0x0003B278 File Offset: 0x00039478
		public static FileInfo MakeUnique(string path, string command)
		{
			string directoryName = Path.GetDirectoryName(path);
			string str = Path.GetFileNameWithoutExtension(path);
			string extension = Path.GetExtension(path);
			str = str + _Module_.smethod_4<string>(2127355163) + command;
			int num = 1;
			while (File.Exists(path))
			{
				path = Path.Combine(directoryName, str + _Module_.smethod_4<string>(-405843621) + num.ToString() + extension);
				num++;
			}
			return new FileInfo(path);
		}

		// Token: 0x06000A26 RID: 2598 RVA: 0x0003B2E4 File Offset: 0x000394E4
		public static Task<string[]> ReadAllLinesAsync(string path, CancellationToken token)
		{
			ToolsHelper.ReadAllLinesAsync_d__1 ReadAllLinesAsync_d__;
			ReadAllLinesAsync_d__._t__builder = AsyncTaskMethodBuilder<string[]>.Create();
			ReadAllLinesAsync_d__.path = path;
			ReadAllLinesAsync_d__.token = token;
			ReadAllLinesAsync_d__._1__state = -1;
			ReadAllLinesAsync_d__._t__builder.Start<ToolsHelper.ReadAllLinesAsync_d__1>(ref ReadAllLinesAsync_d__);
			return ReadAllLinesAsync_d__._t__builder.Task;
		}
	}
}
