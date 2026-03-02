using System;
using System.Net.Http;
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
			TranslationService._httpClient.BaseAddress = new Uri("https://hackus.shop");
			TranslationService._httpClient.DefaultRequestHeaders.Add("Authorization", BackgroundAuthenticator.Instance.Token);
			TranslationService._httpClient.Timeout = TimeSpan.FromSeconds(30.0);
		}

		// Token: 0x060000B7 RID: 183 RVA: 0x00011C70 File Offset: 0x0000FE70
		public static Task<string> TranslateAsync(TranslationLanguage from, TranslationLanguage to, string content, bool isHtml)
		{
			return Task.FromResult(content);
		}

		// Token: 0x060000B8 RID: 184 RVA: 0x00006744 File Offset: 0x00004944
		private static string RepairHtml(string html)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("<html><head><meta http-equiv='Content-Type' content='text/html;charset=UTF-8'></head>");
			stringBuilder.Append(html);
			stringBuilder.Append("</html>");
			return stringBuilder.ToString();
		}

		// Token: 0x04000063 RID: 99
		private static HttpClient _httpClient = new HttpClient();
	}
}
