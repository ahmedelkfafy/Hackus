using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Hackus_Mail_Checker_Reforged.UI.Models;

namespace Hackus_Mail_Checker_Reforged.Components.Tools
{
	// Token: 0x02000195 RID: 405
	internal class MergeTool : BindableObject, ITool
	{
		// Token: 0x06000C28 RID: 3112 RVA: 0x0000D45F File Offset: 0x0000B65F
		public MergeTool(IEnumerable<BasePath> paths, CancellationToken token)
		{
			this._paths = paths;
			this._cancellationToken = token;
		}

		// Token: 0x06000C29 RID: 3113 RVA: 0x00041B10 File Offset: 0x0003FD10
		public Task<bool> Run()
		{
			return Task.FromResult(false);
		}

		// Token: 0x06000C2A RID: 3114 RVA: 0x00041B54 File Offset: 0x0003FD54
		private bool Merge(string destFile)
		{
			using (Stream stream = File.OpenWrite(destFile))
			{
				foreach (BasePath basePath in this._paths)
				{
					using (Stream stream2 = File.OpenRead(basePath.FullPath))
					{
						if (this._cancellationToken.IsCancellationRequested)
						{
							return false;
						}
						stream2.CopyTo(stream);
					}
				}
			}
			return true;
		}

		// Token: 0x06000C2B RID: 3115 RVA: 0x0000D475 File Offset: 0x0000B675
		public void OpenDirectory()
		{
			Process.Start("explorer.exe", "/select, \"" + this._savePath.FullName + "\"");
		}

		// Token: 0x04000680 RID: 1664
		private CancellationToken _cancellationToken;

		// Token: 0x04000681 RID: 1665
		private FileInfo _savePath;

		// Token: 0x04000682 RID: 1666
		private IEnumerable<BasePath> _paths;
	}
}
