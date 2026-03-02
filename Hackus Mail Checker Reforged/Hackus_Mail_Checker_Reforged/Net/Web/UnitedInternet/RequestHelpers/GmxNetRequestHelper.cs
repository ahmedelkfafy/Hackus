using System;
using System.Collections.Generic;

namespace Hackus_Mail_Checker_Reforged.Net.Web.UnitedInternet.RequestHelpers
{
	// Token: 0x020000CA RID: 202
	public class GmxNetRequestHelper : RequestHelper
	{
		// Token: 0x1700015B RID: 347
		// (get) Token: 0x06000653 RID: 1619 RVA: 0x00009F98 File Offset: 0x00008198
		public override string BaseURL
		{
			get
			{
				return "gmx.net";
			}
		}

		// Token: 0x1700015C RID: 348
		// (get) Token: 0x06000654 RID: 1620 RVA: 0x00009FA4 File Offset: 0x000081A4
		public override string LoginURL
		{
			get
			{
				return "https://login.gmx.net/login";
			}
		}

		// Token: 0x1700015D RID: 349
		// (get) Token: 0x06000655 RID: 1621 RVA: 0x00009FB0 File Offset: 0x000081B0
		public override string SuccessURL
		{
			get
			{
				return "https://navigator.gmx.net/login";
			}
		}

		// Token: 0x1700015E RID: 350
		// (get) Token: 0x06000656 RID: 1622 RVA: 0x00009FBC File Offset: 0x000081BC
		public override string ApiURL
		{
			get
			{
				return "3c.gmx.net";
			}
		}

		// Token: 0x1700015F RID: 351
		// (get) Token: 0x06000657 RID: 1623 RVA: 0x00009FC8 File Offset: 0x000081C8
		public override string NavigatorURL
		{
			get
			{
				return "navigator.gmx.net";
			}
		}

		// Token: 0x06000658 RID: 1624 RVA: 0x0002B5A0 File Offset: 0x000297A0
		public override void SetLoginParameters(IList<KeyValuePair<string, string>> parameters)
		{
			parameters.Add(new KeyValuePair<string, string>("service", "freemail"));
			parameters.Add(new KeyValuePair<string, string>("successURL", "https://navigator.gmx.net/login"));
			parameters.Add(new KeyValuePair<string, string>("loginFailedURL", "https://www.gmx.net/login/?status=login-failed"));
			parameters.Add(new KeyValuePair<string, string>("loginErrorURL", "https://www.gmx.net/login/error/loginerror"));
		}

		// Token: 0x04000349 RID: 841
		public const string BASE_URL = "gmx.net";

		// Token: 0x0400034A RID: 842
		public const string API_URL = "3c.gmx.net";

		// Token: 0x0400034B RID: 843
		public const string NAVIGATOR_URL = "navigator.gmx.net";

		// Token: 0x0400034C RID: 844
		public const string LOGIN_URL = "https://login.gmx.net/login";

		// Token: 0x0400034D RID: 845
		public const string SUCCESS_URL = "https://navigator.gmx.net/login";
	}
}
