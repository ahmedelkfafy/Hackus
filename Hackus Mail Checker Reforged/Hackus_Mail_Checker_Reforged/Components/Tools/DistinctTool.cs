using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Hackus_Mail_Checker_Reforged.UI.Models;

namespace Hackus_Mail_Checker_Reforged.Components.Tools
{
	// Token: 0x0200019A RID: 410
	internal class DistinctTool : BindableObject, ITool
	{
		// Token: 0x06000C38 RID: 3128 RVA: 0x0000D53D File Offset: 0x0000B73D
		public DistinctTool(BasePath path, CancellationToken token)
		{
			this._srcPath = path;
			this._cancellationToken = token;
		}

		// Token: 0x06000C39 RID: 3129 RVA: 0x00041D24 File Offset: 0x0003FF24
		public Task<bool> Run()
		{
			return Task.FromResult(false);
		}

		// Token: 0x06000C3A RID: 3130 RVA: 0x0000D553 File Offset: 0x0000B753
		public void OpenDirectory()
		{
			Process.Start("explorer.exe", "/select, \"" + this._savePath.FullName + "\"");
		}

		// Token: 0x0400068C RID: 1676
		private CancellationToken _cancellationToken;

		// Token: 0x0400068D RID: 1677
		private FileInfo _savePath;

		// Token: 0x0400068E RID: 1678
		private BasePath _srcPath;
	}
}
