using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Hackus_Mail_Checker_Reforged.UI.Models;

namespace Hackus_Mail_Checker_Reforged.Components.Tools
{
	// Token: 0x0200019E RID: 414
	internal class SortTool : BindableObject, ITool
	{
		// Token: 0x06000C48 RID: 3144 RVA: 0x0000D5CC File Offset: 0x0000B7CC
		public SortTool(BasePath path, CancellationToken token)
		{
			this._srcPath = path;
			this._cancellationToken = token;
		}

		// Token: 0x06000C49 RID: 3145 RVA: 0x000420E8 File Offset: 0x000402E8
		public Task<bool> Run()
		{
			return Task.FromResult(false);
		}

		// Token: 0x06000C4A RID: 3146 RVA: 0x0000D5E2 File Offset: 0x0000B7E2
		public void OpenDirectory()
		{
			Process.Start("explorer.exe", "/select, \"" + this._savePath.FullName + "\"");
		}

		// Token: 0x0400069C RID: 1692
		private CancellationToken _cancellationToken;

		// Token: 0x0400069D RID: 1693
		private FileInfo _savePath;

		// Token: 0x0400069E RID: 1694
		private BasePath _srcPath;
	}
}
