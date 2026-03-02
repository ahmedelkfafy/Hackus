using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Hackus_Mail_Checker_Reforged.Components.Viewer;
using Hackus_Mail_Checker_Reforged.Services.Background;

namespace Hackus_Mail_Checker_Reforged
{
	// Token: 0x02000022 RID: 34
	public static class TranslationService
	{
		// Token: 0x060000B6 RID: 182 RVA: 0x00011C04 File Offset: 0x0000FE04
		static TranslationService()
		{
			TranslationService._httpClient.BaseAddress = new Uri(_Module_.smethod_4<string>(1303121119));
			TranslationService._httpClient.DefaultRequestHeaders.Add(_Module_.smethod_3<string>(-1674520012), BackgroundAuthenticator.Instance.Token);
			TranslationService._httpClient.Timeout = TimeSpan.FromSeconds(30.0);
		}

		// Token: 0x060000B7 RID: 183 RVA: 0x00011C70 File Offset: 0x0000FE70
		public static Task<string> TranslateAsync(TranslationLanguage from, TranslationLanguage to, string content, bool isHtml)
		{
			TranslationService.TranslateAsync_d__2 TranslateAsync_d__;
			TranslateAsync_d__._t__builder = AsyncTaskMethodBuilder<string>.Create();
			TranslateAsync_d__.from = from;
			TranslateAsync_d__.to = to;
			TranslateAsync_d__.content = content;
			TranslateAsync_d__.isHtml = isHtml;
			TranslateAsync_d__._1__state = -1;
			TranslateAsync_d__._t__builder.Start<TranslationService.TranslateAsync_d__2>(ref TranslateAsync_d__);
			return TranslateAsync_d__._t__builder.Task;
		}

		// Token: 0x060000B8 RID: 184 RVA: 0x00006744 File Offset: 0x00004944
		private static string RepairHtml(string html)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(_Module_.smethod_3<string>(-229394219));
			stringBuilder.Append(html);
			stringBuilder.Append(_Module_.smethod_2<string>(1319412416));
			return stringBuilder.ToString();
		}

		// Token: 0x04000063 RID: 99
		private static HttpClient _httpClient = new HttpClient();
	}
}
