using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hackus_Mail_Checker_Reforged.Models;
using Hackus_Mail_Checker_Reforged.Services.Background;
using Newtonsoft.Json;

namespace Hackus_Mail_Checker_Reforged.Services.Shared
{
	// Token: 0x0200006A RID: 106
	public static class ReportService
	{
		// Token: 0x060003B4 RID: 948 RVA: 0x00017ACC File Offset: 0x00015CCC
		static ReportService()
		{
			ReportService._httpClient.BaseAddress = new Uri("https://hackus.shop");
			ReportService._httpClient.DefaultRequestHeaders.Add("Authorization", BackgroundAuthenticator.Instance.Token);
			ReportService._httpClient.Timeout = TimeSpan.FromSeconds(30.0);
			ReportService._locker = new object();
			ReportService._foundServers = new HashSet<Server>();
		}

		// Token: 0x060003B5 RID: 949 RVA: 0x00017B4C File Offset: 0x00015D4C
		public static void AddServer(Server server)
		{
			object locker = ReportService._locker;
			lock (locker)
			{
				ReportService._foundServers.Add(server);
			}
		}

		// Token: 0x060003B6 RID: 950 RVA: 0x00008CA1 File Offset: 0x00006EA1
		public static void ReportServers()
		{
			if (!ReportService._foundServers.Any<Server>())
			{
				return;
			}
			Task.Run(delegate()
			{
				try
				{
					StringContent httpContent_ = null;
					object locker = ReportService._locker;
					bool flag = false;
					try
					{
						Monitor.Enter(locker, ref flag);
						httpContent_ = new StringContent(JsonConvert.SerializeObject(ReportService._foundServers), Encoding.UTF8, "application/json");
						ReportService._foundServers.Clear();
					}
					finally
					{
						if (flag)
						{
							Monitor.Exit(locker);
						}
					}
					ReportService._httpClient.PostAsync("api/report/sendServers", httpContent_).GetAwaiter().GetResult();
				}
				catch
				{
				}
			});
		}

		// Token: 0x04000214 RID: 532
		private static object _locker;

		// Token: 0x04000215 RID: 533
		private static HashSet<Server> _foundServers;

		// Token: 0x04000216 RID: 534
		private static HttpClient _httpClient = new HttpClient();
	}
}
