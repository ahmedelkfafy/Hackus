using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Hackus_Mail_Checker_Reforged.Models.Enums;
using Hackus_Mail_Checker_Reforged.Services.Managers;
using Hackus_Mail_Checker_Reforged.Services.Settings;
using xNet;

namespace Hackus_Mail_Checker_Reforged.Net.Captcha
{
	// Token: 0x0200012F RID: 303
	internal class RuCaptcha : ICaptchaClient
	{
		// Token: 0x06000972 RID: 2418 RVA: 0x0003A134 File Offset: 0x00038334
				public ValueTuple<OperationResult, string> GetBalance()
		{
			try
			{
				using (HttpRequest httpRequest = new HttpRequest())
				{
					this.SetHeaders(httpRequest);
					httpRequest.AddUrlParam("key", WebSettings.Instance.CaptchaSolvationKey);
					httpRequest.AddUrlParam("action", "getbalance");
					string text = httpRequest.Post("http://rucaptcha.com/res.php").ToString();
					try
					{
						double.Parse(text);
						return new ValueTuple<OperationResult, string>(OperationResult.Ok, text + " ₽");
					}
					catch
					{
						return new ValueTuple<OperationResult, string>(OperationResult.Error, text);
					}
				}
			}
			catch
			{
			}
			return new ValueTuple<OperationResult, string>(OperationResult.HttpError, null);
		}

		// Token: 0x06000973 RID: 2419 RVA: 0x0003A204 File Offset: 0x00038404
				public ValueTuple<OperationResult, string> SolveCaptcha(string base64, string lang, bool onlyLetters = false)
		{
			ValueTuple<OperationResult, string> valueTuple = this.SendCaptchaRequest(base64, lang, onlyLetters);
			OperationResult item = valueTuple.Item1;
			string item2 = valueTuple.Item2;
			if (item == OperationResult.Ok)
			{
				return this.GetResult(item2);
			}
			return new ValueTuple<OperationResult, string>(item, null);
		}

		// Token: 0x06000974 RID: 2420 RVA: 0x0003A23C File Offset: 0x0003843C
				public ValueTuple<OperationResult, string> SolveRecaptchaV2Proxyless(string siteKey, string pageUrl)
		{
			ValueTuple<OperationResult, string> valueTuple = this.SendRecaptchaV2ProxylessRequest(siteKey, pageUrl);
			OperationResult item = valueTuple.Item1;
			string item2 = valueTuple.Item2;
			if (item == OperationResult.Ok)
			{
				return this.GetResult(item2);
			}
			return new ValueTuple<OperationResult, string>(item, null);
		}

		// Token: 0x06000975 RID: 2421 RVA: 0x0003A270 File Offset: 0x00038470
				public ValueTuple<OperationResult, string> SolveHCaptcha(string siteKey, string pageUrl, string userAgent)
		{
			ValueTuple<OperationResult, string> valueTuple = this.SendHcaptchaRequest(siteKey, pageUrl);
			OperationResult item = valueTuple.Item1;
			string item2 = valueTuple.Item2;
			if (item != OperationResult.Ok)
			{
				return new ValueTuple<OperationResult, string>(item, null);
			}
			return this.GetResult(item2);
		}

		// Token: 0x06000976 RID: 2422 RVA: 0x0003A2A4 File Offset: 0x000384A4
				private ValueTuple<OperationResult, string> SendCaptchaRequest(string base64, string lang, bool onlyLetters)
		{
			for (int i = 0; i < 3; i++)
			{
				this.WaitPause();
				try
				{
					using (HttpRequest httpRequest = new HttpRequest())
					{
						this.SetHeaders(httpRequest);
						httpRequest.AddParam("key", WebSettings.Instance.CaptchaSolvationKey);
						httpRequest.AddParam("method", "base64");
						httpRequest.AddParam("body", base64);
						if (lang != null)
						{
							httpRequest.AddParam("lang", lang);
						}
						if (onlyLetters)
						{
							httpRequest.AddParam("numeric", 2);
						}
						string text = httpRequest.Post("http://rucaptcha.com/in.php").ToString();
						if (text.Contains("OK"))
						{
							string[] array = text.Split(new char[]
							{
								'|'
							});
							return new ValueTuple<OperationResult, string>(OperationResult.Ok, array[1]);
						}
						if (!text.Contains("MAX_USER_TURN") && !text.Contains("ERROR_NO_SLOT_AVAILABLE"))
						{
							return new ValueTuple<OperationResult, string>(OperationResult.Error, null);
						}
						Thread.Sleep(5000);
					}
				}
				catch
				{
				}
			}
			return new ValueTuple<OperationResult, string>(OperationResult.HttpError, null);
		}

		// Token: 0x06000977 RID: 2423 RVA: 0x0003A41C File Offset: 0x0003861C
				private ValueTuple<OperationResult, string> SendRecaptchaV2ProxylessRequest(string siteKey, string pageUrl)
		{
			for (int i = 0; i < 3; i++)
			{
				this.WaitPause();
				try
				{
					using (HttpRequest httpRequest = new HttpRequest())
					{
						this.SetHeaders(httpRequest);
						httpRequest.AddParam("key", WebSettings.Instance.CaptchaSolvationKey);
						httpRequest.AddParam("method", "userrecaptcha");
						httpRequest.AddParam("googlekey", siteKey);
						httpRequest.AddParam("pageurl", pageUrl);
						string text = httpRequest.Post("http://rucaptcha.com/in.php").ToString();
						if (text.Contains("OK"))
						{
							string[] array = text.Split(new char[]
							{
								'|'
							});
							return new ValueTuple<OperationResult, string>(OperationResult.Ok, array[1]);
						}
						if (!text.Contains("MAX_USER_TURN") && !text.Contains("ERROR_NO_SLOT_AVAILABLE"))
						{
							return new ValueTuple<OperationResult, string>(OperationResult.Error, null);
						}
						Thread.Sleep(5000);
					}
				}
				catch
				{
				}
			}
			return new ValueTuple<OperationResult, string>(OperationResult.HttpError, null);
		}

		// Token: 0x06000978 RID: 2424 RVA: 0x0003A560 File Offset: 0x00038760
				private ValueTuple<OperationResult, string> SendHcaptchaRequest(string siteKey, string pageUrl)
		{
			for (int i = 0; i < 3; i++)
			{
				this.WaitPause();
				try
				{
					using (HttpRequest httpRequest = new HttpRequest())
					{
						this.SetHeaders(httpRequest);
						httpRequest.AddParam("key", WebSettings.Instance.CaptchaSolvationKey);
						httpRequest.AddParam("method", "hcaptcha");
						httpRequest.AddParam("siteKey", siteKey);
						httpRequest.AddParam("pageurl", pageUrl);
						string text = httpRequest.Post("http://rucaptcha.com/in.php").ToString();
						if (text.Contains("OK"))
						{
							string[] array = text.Split(new char[]
							{
								'|'
							});
							return new ValueTuple<OperationResult, string>(OperationResult.Ok, array[1]);
						}
						if (!text.Contains("MAX_USER_TURN") && !text.Contains("ERROR_NO_SLOT_AVAILABLE"))
						{
							return new ValueTuple<OperationResult, string>(OperationResult.Error, null);
						}
						Thread.Sleep(5000);
					}
				}
				catch
				{
				}
			}
			return new ValueTuple<OperationResult, string>(OperationResult.HttpError, null);
		}

		// Token: 0x06000979 RID: 2425 RVA: 0x0003A6A0 File Offset: 0x000388A0
				private ValueTuple<OperationResult, string> GetResult(string id)
		{
			if (id == null)
			{
				return new ValueTuple<OperationResult, string>(OperationResult.Error, null);
			}
			for (int i = 0; i < 20; i++)
			{
				this.WaitPause();
				try
				{
					using (HttpRequest httpRequest = new HttpRequest())
					{
						this.SetHeaders(httpRequest);
						httpRequest.AddUrlParam("key", WebSettings.Instance.CaptchaSolvationKey);
						httpRequest.AddUrlParam("action", "get");
						httpRequest.AddUrlParam("id", id);
						string text = httpRequest.Get("http://rucaptcha.com/res.php", null).ToString();
						if (text.Contains("OK"))
						{
							string[] array = text.Split(new char[]
							{
								'|'
							});
							return new ValueTuple<OperationResult, string>(OperationResult.Ok, array[1]);
						}
						if (!text.Contains("CAPCHA_NOT_READY"))
						{
							return new ValueTuple<OperationResult, string>(OperationResult.Error, null);
						}
						Thread.Sleep(10000);
					}
				}
				catch
				{
				}
			}
			return new ValueTuple<OperationResult, string>(OperationResult.HttpError, null);
		}

		// Token: 0x0600097A RID: 2426 RVA: 0x0000BAF5 File Offset: 0x00009CF5
		private void SetHeaders(HttpRequest request)
		{
			request.IgnoreProtocolErrors = true;
			request.ConnectTimeout = CheckerSettings.Instance.Timeout * 1000;
		}

		// Token: 0x0600097B RID: 2427 RVA: 0x00009AC8 File Offset: 0x00007CC8
		private void WaitPause()
		{
			ThreadsManager.Instance.WaitHandle.WaitOne();
		}
	}
}
