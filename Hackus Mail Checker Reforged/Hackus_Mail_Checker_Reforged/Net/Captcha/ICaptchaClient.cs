using System;
using System.Runtime.CompilerServices;
using Hackus_Mail_Checker_Reforged.Models.Enums;

namespace Hackus_Mail_Checker_Reforged.Net.Captcha
{
	// Token: 0x0200012E RID: 302
	internal interface ICaptchaClient
	{
		// Token: 0x0600096E RID: 2414
				ValueTuple<OperationResult, string> SolveCaptcha(string base64, string lang, bool onlyLetters = false);

		// Token: 0x0600096F RID: 2415
				ValueTuple<OperationResult, string> SolveRecaptchaV2Proxyless(string siteKey, string pageUrl);

		// Token: 0x06000970 RID: 2416
				ValueTuple<OperationResult, string> SolveHCaptcha(string siteKey, string pageUrl, string userAgent);

		// Token: 0x06000971 RID: 2417
				ValueTuple<OperationResult, string> GetBalance();
	}
}
