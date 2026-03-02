using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using Hackus_Mail_Checker_Reforged.Models.Enums;
using Hackus_Mail_Checker_Reforged.Services.Managers;
using Hackus_Mail_Checker_Reforged.Services.Settings;
using xNet;

namespace Hackus_Mail_Checker_Reforged.Net.Captcha
{
	// Token: 0x0200012D RID: 301
	internal class AntiCaptcha : ICaptchaClient
	{
		// Token: 0x06000963 RID: 2403 RVA: 0x00039990 File Offset: 0x00037B90
				public ValueTuple<OperationResult, string> GetBalance()
		{
			try
			{
				using (HttpRequest httpRequest = new HttpRequest())
				{
					this.SetHeaders(httpRequest);
					string text = "{\"clientKey\":\"" + WebSettings.Instance.CaptchaSolvationKey + "\"}";
					string text2 = httpRequest.Post("http://api.anti-captcha.com/getBalance", text, "application/json").ToString();
					if (text2.Contains("\"errorId\":0") || text2.Contains("\"errorId\": 0"))
					{
						Match match = Regex.Match(text2, "\"balance\":(.+?)}");
						if (match.Success)
						{
							return new ValueTuple<OperationResult, string>(OperationResult.Ok, match.Groups[1].Value + " USD");
						}
					}
					Match match2 = Regex.Match(text2, "\"errorDescription\":\"(.+?)\"");
					if (match2.Success)
					{
						return new ValueTuple<OperationResult, string>(OperationResult.Error, match2.Groups[1].Value);
					}
					return new ValueTuple<OperationResult, string>(OperationResult.Error, "Unknown error");
				}
			}
			catch
			{
			}
			return new ValueTuple<OperationResult, string>(OperationResult.HttpError, null);
		}

		// Token: 0x06000964 RID: 2404 RVA: 0x00039AEC File Offset: 0x00037CEC
				public ValueTuple<OperationResult, string> SolveCaptcha(string base64, string lang, bool onlyLetters = false)
		{
			if (lang == null)
			{
				lang = "en";
			}
			string lang2 = (lang == "ru") ? "rn" : lang;
			ValueTuple<OperationResult, string> valueTuple = this.SendCaptchaRequest(base64, lang2, onlyLetters);
			OperationResult item = valueTuple.Item1;
			string item2 = valueTuple.Item2;
			if (item == OperationResult.Ok)
			{
				return this.GetResult(item2);
			}
			return new ValueTuple<OperationResult, string>(item, null);
		}

		// Token: 0x06000965 RID: 2405 RVA: 0x00039B50 File Offset: 0x00037D50
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

		// Token: 0x06000966 RID: 2406 RVA: 0x00039B84 File Offset: 0x00037D84
				public ValueTuple<OperationResult, string> SolveHCaptcha(string siteKey, string pageUrl, string userAgent)
		{
			ValueTuple<OperationResult, string> valueTuple = this.SendHCaptchaRequest(siteKey, pageUrl, userAgent);
			OperationResult item = valueTuple.Item1;
			string item2 = valueTuple.Item2;
			if (item == OperationResult.Ok)
			{
				return this.GetResult(item2);
			}
			return new ValueTuple<OperationResult, string>(item, null);
		}

		// Token: 0x06000967 RID: 2407 RVA: 0x00039BBC File Offset: 0x00037DBC
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
						string text = string.Concat(new string[]
						{
							"{\"languagePool\":\"",
							lang,
							"\",\"clientKey\":\"",
							WebSettings.Instance.CaptchaSolvationKey,
							"\",\"task\":{\"type\":\"ImageToTextTask\",\"body\":\"",
							base64,
							"\",\"numeric\":",
							(onlyLetters ? 2 : 0).ToString(),
							"}}"
						});
						string text2 = httpRequest.Post("http://api.anti-captcha.com/createTask", text, "application/json").ToString();
						if (text2.Contains("\"errorId\":0"))
						{
							Match match = Regex.Match(text2, "\"taskId\":(.+?)}");
							if (match.Success)
							{
								return new ValueTuple<OperationResult, string>(OperationResult.Ok, match.Groups[1].Value);
							}
						}
						return new ValueTuple<OperationResult, string>(OperationResult.Error, null);
					}
				}
				catch
				{
				}
			}
			return new ValueTuple<OperationResult, string>(OperationResult.HttpError, null);
		}

		// Token: 0x06000968 RID: 2408 RVA: 0x00039D28 File Offset: 0x00037F28
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
						string text = string.Concat(new string[]
						{
							"{\"clientKey\":\"",
							WebSettings.Instance.CaptchaSolvationKey,
							"\",\"task\":{\"type\":\"RecaptchaV2TaskProxyless\",\"websiteURL\":\"",
							pageUrl,
							"\",\"websiteKey\":\"",
							siteKey,
							"\"}}"
						});
						string text2 = httpRequest.Post("http://api.anti-captcha.com/createTask", text, "application/json").ToString();
						if (text2.Contains("\"errorId\":0"))
						{
							Match match = Regex.Match(text2, "\"taskId\":(.+?)}");
							if (match.Success)
							{
								return new ValueTuple<OperationResult, string>(OperationResult.Ok, match.Groups[1].Value);
							}
						}
						return new ValueTuple<OperationResult, string>(OperationResult.Error, null);
					}
				}
				catch
				{
				}
			}
			return new ValueTuple<OperationResult, string>(OperationResult.HttpError, null);
		}

		// Token: 0x06000969 RID: 2409 RVA: 0x00039E58 File Offset: 0x00038058
				private ValueTuple<OperationResult, string> SendHCaptchaRequest(string siteKey, string pageUrl, string userAgent)
		{
			for (int i = 0; i < 3; i++)
			{
				this.WaitPause();
				try
				{
					using (HttpRequest httpRequest = new HttpRequest())
					{
						this.SetHeaders(httpRequest);
						string text = string.Concat(new string[]
						{
							"{\"clientKey\":\"",
							WebSettings.Instance.CaptchaSolvationKey,
							"\",\"task\":{\"type\":\"HCaptchaTaskProxyless\",\"websiteURL\":\"",
							pageUrl,
							"\",\"websiteKey\":\"",
							siteKey,
							"\",\"userAgent\":\"",
							userAgent,
							"\"}}"
						});
						string text2 = httpRequest.Post("http://api.anti-captcha.com/createTask", text, "application/json").ToString();
						if (text2.Contains("\"errorId\":0"))
						{
							Match match = Regex.Match(text2, "\"taskId\":(.+?)}");
							if (match.Success)
							{
								return new ValueTuple<OperationResult, string>(OperationResult.Ok, match.Groups[1].Value);
							}
						}
						return new ValueTuple<OperationResult, string>(OperationResult.Error, null);
					}
				}
				catch
				{
				}
			}
			return new ValueTuple<OperationResult, string>(OperationResult.HttpError, null);
		}

		// Token: 0x0600096A RID: 2410 RVA: 0x00039F9C File Offset: 0x0003819C
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
						string text = string.Concat(new string[]
						{
							"{\"clientKey\":\"",
							WebSettings.Instance.CaptchaSolvationKey,
							"\",\"taskId\":",
							id,
							"}"
						});
						string text2 = httpRequest.Post("http://api.anti-captcha.com/getTaskResult", text, "application/json").ToString();
						if (text2.Contains("\"status\":\"ready\""))
						{
							Match match = Regex.Match(text2, "\"gRecaptchaResponse\":\"(.+?)\"");
							if (match.Success)
							{
								return new ValueTuple<OperationResult, string>(OperationResult.Ok, match.Groups[1].Value);
							}
							match = Regex.Match(text2, "\"text\":\"(.+?)\"");
							if (match.Success)
							{
								return new ValueTuple<OperationResult, string>(OperationResult.Ok, match.Groups[1].Value);
							}
						}
						else if (text2.Contains("\"status\":\"processing\""))
						{
							Thread.Sleep(10000);
							goto IL_143;
						}
						return new ValueTuple<OperationResult, string>(OperationResult.Error, null);
					}
				}
				catch
				{
				}
				IL_143:;
			}
			return new ValueTuple<OperationResult, string>(OperationResult.HttpError, null);
		}

		// Token: 0x0600096B RID: 2411 RVA: 0x0000BAF5 File Offset: 0x00009CF5
		private void SetHeaders(HttpRequest request)
		{
			request.IgnoreProtocolErrors = true;
			request.ConnectTimeout = CheckerSettings.Instance.Timeout * 1000;
		}

		// Token: 0x0600096C RID: 2412 RVA: 0x00009AC8 File Offset: 0x00007CC8
		private void WaitPause()
		{
			ThreadsManager.Instance.WaitHandle.WaitOne();
		}
	}
}
