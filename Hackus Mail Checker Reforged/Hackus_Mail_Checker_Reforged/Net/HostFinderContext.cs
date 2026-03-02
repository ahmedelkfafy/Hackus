using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using DnsClient;
using DnsClient.Protocol;
using Hackus_Mail_Checker_Reforged.Models;
using Hackus_Mail_Checker_Reforged.Models.Enums;
using Hackus_Mail_Checker_Reforged.Net.Mail;
using Hackus_Mail_Checker_Reforged.Net.Mail.IMAP;
using Hackus_Mail_Checker_Reforged.Net.Mail.POP3;
using Hackus_Mail_Checker_Reforged.Net.Mail.Utilities;
using Hackus_Mail_Checker_Reforged.Services.Managers;
using Hackus_Mail_Checker_Reforged.Services.Settings;
using xNet;

namespace Hackus_Mail_Checker_Reforged.Net
{
	// Token: 0x02000099 RID: 153
	public class HostFinderContext
	{
		// Token: 0x1700012B RID: 299
		// (get) Token: 0x060004F9 RID: 1273 RVA: 0x00009974 File Offset: 0x00007B74
		public string Domain
		{
			get
			{
				return this._domain;
			}
		}

		// Token: 0x1700012C RID: 300
		// (get) Token: 0x060004FA RID: 1274 RVA: 0x0001BB94 File Offset: 0x00019D94
		public bool HasWork
		{
			get
			{
				object contextLocker = this._contextLocker;
				bool result;
				lock (contextLocker)
				{
					if (this._serversToPing != null && this._serversToPing.Any<Server>())
					{
						result = true;
					}
					else if (this._webConfigs != null && this._webConfigs.Any<HostFinderContext.WebConfig>())
					{
						result = true;
					}
					else
					{
						result = false;
					}
				}
				return result;
			}
		}

		// Token: 0x060004FB RID: 1275 RVA: 0x0000997C File Offset: 0x00007B7C
		public HostFinderContext(string domain)
		{
			this._domain = domain;
			this._serversToPing = new ConcurrentQueue<Server>();
			this._webConfigs = new ConcurrentQueue<HostFinderContext.WebConfig>();
		}

		// Token: 0x060004FC RID: 1276 RVA: 0x0001BC04 File Offset: 0x00019E04
		private void Start()
		{
			object contextLocker = this._contextLocker;
			lock (contextLocker)
			{
				this.IsStarted = true;
				Server server = this.FindCoincidence();
				if (server != null)
				{
					this.IsHandled = true;
					this.Result = server;
					return;
				}
			}
			if (CheckerSettings.Instance.GuessSubdomainByDomain)
			{
				foreach (Server item in this.GenerateServersByDomain(this._domain))
				{
					this._serversToPing.Enqueue(item);
				}
			}
			if (CheckerSettings.Instance.SearchServerByMX)
			{
				string text;
				if (CheckerSettings.Instance.UseProxyToSearchServer)
				{
					text = this.FindMxRecordByGoogle();
				}
				else
				{
					text = this.FindMXRecordByLookup();
				}
				if (text != null)
				{
					Server server2 = this.FindCoincidence(text);
					if (server2 != null)
					{
						this.IsHandled = true;
						this.Result = server2;
						return;
					}
					if (CheckerSettings.Instance.GuessSubdomainByMX)
					{
						foreach (Server item2 in this.GenerateServersByMx(text))
						{
							this._serversToPing.Enqueue(item2);
						}
						this._serversToPing = new ConcurrentQueue<Server>(this._serversToPing.Distinct<Server>());
					}
				}
			}
			if (CheckerSettings.Instance.SearchServerByAutoConfig)
			{
				this._webConfigs.Enqueue(HostFinderContext.WebConfig.AutoConfig);
			}
			if (CheckerSettings.Instance.SearchServerByAutoDiscover)
			{
				this._webConfigs.Enqueue(HostFinderContext.WebConfig.AutoDiscovery);
			}
		}

		// Token: 0x060004FD RID: 1277 RVA: 0x0001BDB4 File Offset: 0x00019FB4
		public void Handle()
		{
			bool flag = false;
			if (!this.IsStarted)
			{
				this.Start();
				flag = true;
			}
			while (!this.IsHandled)
			{
				Server server = this.GetServerWorkItem();
				if (server == null)
				{
					HostFinderContext.WebConfig webConfigWorkItem = this.GetWebConfigWorkItem();
					if (webConfigWorkItem == HostFinderContext.WebConfig.None)
					{
						if (flag)
						{
							while (this._inProcess > 0)
							{
								Thread.Sleep(1000);
							}
							if (this.Result != null)
							{
								ConfigurationManager.Instance.Configuration.Add(this.Result);
							}
							this.IsHandled = true;
							return;
						}
						if (!this.HasWork)
						{
							return;
						}
					}
					else
					{
						Interlocked.Increment(ref this._inProcess);
						if (webConfigWorkItem != HostFinderContext.WebConfig.AutoConfig)
						{
							server = this.FindServerByAutoDiscover("", 0);
						}
						else
						{
							server = this.FindServerByAutoConfig("", 0);
						}
						if (server != null && this.Result == null)
						{
							this.IsHandled = true;
							this.Result = server;
							Interlocked.Decrement(ref this._inProcess);
							return;
						}
						Interlocked.Decrement(ref this._inProcess);
					}
				}
				else
				{
					Interlocked.Increment(ref this._inProcess);
					if (this.ConnectToServer(server) && this.Result == null)
					{
						this.IsHandled = true;
						this.Result = server;
						Interlocked.Decrement(ref this._inProcess);
						return;
					}
					Interlocked.Decrement(ref this._inProcess);
				}
			}
		}

		// Token: 0x060004FE RID: 1278 RVA: 0x0001BEEC File Offset: 0x0001A0EC
		private HostFinderContext.WebConfig GetWebConfigWorkItem()
		{
			HostFinderContext.WebConfig result = HostFinderContext.WebConfig.None;
			object contextLocker = this._contextLocker;
			lock (contextLocker)
			{
				if (this._webConfigs != null)
				{
					this._webConfigs.TryDequeue(out result);
				}
			}
			return result;
		}

		// Token: 0x060004FF RID: 1279 RVA: 0x0001BF40 File Offset: 0x0001A140
		private Server GetServerWorkItem()
		{
			Server result = null;
			object contextLocker = this._contextLocker;
			lock (contextLocker)
			{
				if (this._serversToPing != null)
				{
					this._serversToPing.TryDequeue(out result);
				}
			}
			return result;
		}

		// Token: 0x06000500 RID: 1280 RVA: 0x0001BF94 File Offset: 0x0001A194
		private List<Server> GenerateServersByDomain(string domain)
		{
			List<Server> list = new List<Server>();
			foreach (string str in HostFinderContext._imapPrefixes)
			{
				list.Add(new Server
				{
					Domain = this._domain,
					Hostname = str + domain,
					Port = 993,
					Socket = Hackus_Mail_Checker_Reforged.Models.Enums.SocketType.SSL,
					Protocol = Hackus_Mail_Checker_Reforged.Models.Enums.ProtocolType.IMAP
				});
				list.Add(new Server
				{
					Domain = this._domain,
					Hostname = str + domain,
					Port = 143,
					Socket = Hackus_Mail_Checker_Reforged.Models.Enums.SocketType.Plain,
					Protocol = Hackus_Mail_Checker_Reforged.Models.Enums.ProtocolType.IMAP
				});
			}
			foreach (string str2 in HostFinderContext._pop3Prefixes)
			{
				list.Add(new Server
				{
					Domain = this._domain,
					Hostname = str2 + domain,
					Port = 995,
					Socket = Hackus_Mail_Checker_Reforged.Models.Enums.SocketType.SSL,
					Protocol = Hackus_Mail_Checker_Reforged.Models.Enums.ProtocolType.POP3
				});
				list.Add(new Server
				{
					Domain = this._domain,
					Hostname = str2 + domain,
					Port = 110,
					Socket = Hackus_Mail_Checker_Reforged.Models.Enums.SocketType.Plain,
					Protocol = Hackus_Mail_Checker_Reforged.Models.Enums.ProtocolType.POP3
				});
			}
			return list;
		}

		// Token: 0x06000501 RID: 1281 RVA: 0x0001C11C File Offset: 0x0001A31C
		private List<Server> GenerateServersByMx(string mx)
		{
			List<Server> list = null;
			string[] array = mx.Split(new char[]
			{
				'.'
			});
			array = (from p in array
			where HostFinderContext._c_.smethod_0(p) > 1
			select p).ToArray<string>();
			int num = array.Length;
			if (num >= 2)
			{
				list = this.GenerateServersByDomain(array[num - 2] + "." + array[num - 1]);
				return (from s in list
				select new Server(this._domain, s.Hostname, s.Port, s.Protocol, s.Socket)).ToList<Server>();
			}
			return list;
		}

		// Token: 0x06000502 RID: 1282 RVA: 0x0001C1AC File Offset: 0x0001A3AC
		private Server FindCoincidence()
		{
			KeyValuePair<string, string> keyValuePair = HostFinderContext._domainCoincidences.FirstOrDefault((KeyValuePair<string, string> d) => this._domain.Contains(d.Key));
			if (keyValuePair.Key != null)
			{
				return new Server
				{
					Domain = this._domain,
					Hostname = keyValuePair.Value,
					Port = 993,
					Protocol = Hackus_Mail_Checker_Reforged.Models.Enums.ProtocolType.IMAP,
					Socket = Hackus_Mail_Checker_Reforged.Models.Enums.SocketType.SSL
				};
			}
			return null;
		}

		// Token: 0x06000503 RID: 1283 RVA: 0x0001C214 File Offset: 0x0001A414
		private Server FindCoincidence(string mx)
		{
			KeyValuePair<string, string> keyValuePair = HostFinderContext._recordCoincidences.FirstOrDefault((KeyValuePair<string, string> r) => HostFinderContext._c__DisplayClass21_0.smethod_0(mx, r.Key));
			if (keyValuePair.Key == null)
			{
				return null;
			}
			return new Server
			{
				Domain = this._domain,
				Hostname = keyValuePair.Value,
				Port = 993,
				Protocol = Hackus_Mail_Checker_Reforged.Models.Enums.ProtocolType.IMAP,
				Socket = Hackus_Mail_Checker_Reforged.Models.Enums.SocketType.SSL
			};
		}

		// Token: 0x06000504 RID: 1284 RVA: 0x0001C288 File Offset: 0x0001A488
		private string FindMXRecordByLookup()
		{
			NameServer nameServer = new NameServer(IPAddress.Parse("208.67.220.220"));
			LookupClient lookupClient = new LookupClient(new LookupClientOptions(new NameServer[]
			{
				nameServer
			})
			{
				ThrowDnsErrors = false,
				ContinueOnDnsError = true
			});
			string result;
			try
			{
				IDnsQueryResponse dnsQueryResponse = lookupClient.Query(this._domain, 15, 1);
				string text;
				if (dnsQueryResponse == null)
				{
					text = null;
				}
				else
				{
					IReadOnlyList<DnsResourceRecord> answers = dnsQueryResponse.Answers;
					if (answers == null)
					{
						text = null;
					}
					else
					{
						IEnumerable<MxRecord> enumerable = RecordCollectionExtension.MxRecords(answers);
						if (enumerable == null)
						{
							text = null;
						}
						else
						{
							MxRecord mxRecord = enumerable.FirstOrDefault<MxRecord>();
							if (mxRecord == null)
							{
								text = null;
							}
							else
							{
								DnsString exchange = mxRecord.Exchange;
								text = ((exchange != null) ? exchange.Value : null);
							}
						}
					}
				}
				result = text;
			}
			catch (ThreadAbortException)
			{
				throw;
			}
			catch
			{
				result = null;
			}
			return result;
		}

		// Token: 0x06000505 RID: 1285 RVA: 0x0001C340 File Offset: 0x0001A540
		public string FindMxRecordByGoogle()
		{
			string result;
			for (;;)
			{
				ThreadsManager.Instance.WaitHandle.WaitOne();
				ProxyClient proxy = ProxyManager.Instance.GetProxy();
				try
				{
					using (HttpRequest httpRequest = new HttpRequest())
					{
						httpRequest.IgnoreProtocolErrors = true;
						httpRequest.ConnectTimeout = 15000;
						httpRequest.Proxy = proxy;
						httpRequest.KeepAlive = false;
						httpRequest.AllowAutoRedirect = false;
						httpRequest.UserAgent = "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)";
						httpRequest.AddUrlParam("name", this._domain);
						httpRequest.AddUrlParam("type", "mx");
						string text = httpRequest.Get("https://dns.google/resolve", null).ToString();
						if (!string.IsNullOrEmpty(text))
						{
							Match match = Regex.Match(text, "\"Answer\":\\[{(.+?)\"data\":\"(.+?) (.+?).\"");
							if (!match.Success)
							{
								result = null;
							}
							else
							{
								result = match.Groups[3].Value;
							}
						}
						else
						{
							result = null;
						}
					}
				}
				catch (ThreadAbortException)
				{
					throw;
				}
				catch
				{
					continue;
				}
				break;
			}
			return result;
		}

		// Token: 0x06000506 RID: 1286 RVA: 0x0001C46C File Offset: 0x0001A66C
		public Server FindServerByAutoConfig(string prefix = "", int errors = 0)
		{
			ProxyClient proxyClient = null;
			if (CheckerSettings.Instance.UseProxyToSearchServer)
			{
				proxyClient = ProxyManager.Instance.GetProxy();
			}
			ThreadsManager.Instance.WaitHandle.WaitOne();
			try
			{
				using (HttpRequest httpRequest = new HttpRequest())
				{
					httpRequest.IgnoreProtocolErrors = true;
					httpRequest.ConnectTimeout = 15000;
					httpRequest.Proxy = proxyClient;
					httpRequest.KeepAlive = false;
					httpRequest.AllowAutoRedirect = false;
					httpRequest.UserAgent = "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)";
					httpRequest.AddUrlParam("emailaddress", "@admin");
					string text = httpRequest.Get("http://" + prefix + this._domain + "/mail/config-v1.1.xml", null).ToString();
					if (string.IsNullOrEmpty(text))
					{
						return null;
					}
					if (text.Contains("<incomingServer"))
					{
						foreach (Match match in Regex.Matches(text, "<incomingServer type=\"(.*?)\">(.*?)<\\/incomingServer>", RegexOptions.Singleline))
						{
							if (match.Success)
							{
								string value = match.Groups[1].Value;
								if (value != "smtp")
								{
									string value2 = match.Groups[2].Value;
									return new Server
									{
										Domain = this._domain,
										Hostname = value2.Substring("<hostname>", "</hostname>", 0, StringComparison.Ordinal, null),
										Port = int.Parse(value2.Substring("<port>", "</port>", 0, StringComparison.Ordinal, null)),
										Socket = ((value2.ContainsIgnoreCase("<Socket>SSL</Socket>") || value2.ContainsIgnoreCase("<socketType>SSL</socketType>")) ? Hackus_Mail_Checker_Reforged.Models.Enums.SocketType.SSL : Hackus_Mail_Checker_Reforged.Models.Enums.SocketType.Plain),
										Protocol = (value.Contains("imap") ? Hackus_Mail_Checker_Reforged.Models.Enums.ProtocolType.IMAP : Hackus_Mail_Checker_Reforged.Models.Enums.ProtocolType.POP3)
									};
								}
							}
						}
					}
					if (string.IsNullOrEmpty(prefix))
					{
						return this.FindServerByAutoConfig("autoconfig.", 0);
					}
				}
			}
			catch (ThreadAbortException)
			{
				throw;
			}
			catch (Exception ex)
			{
				if (ex is HttpException || ex is SocketException || ex is IOException || ex is ProxyException)
				{
					errors++;
					if (CheckerSettings.Instance.UseProxyToSearchServer && proxyClient != null && errors <= 2)
					{
						return this.FindServerByAutoConfig(prefix, errors);
					}
					return null;
				}
			}
			return null;
		}

		// Token: 0x06000507 RID: 1287 RVA: 0x0001C774 File Offset: 0x0001A974
		public Server FindServerByAutoDiscover(string prefix = "", int errors = 0)
		{
			ProxyClient proxyClient = null;
			if (CheckerSettings.Instance.UseProxyToSearchServer)
			{
				proxyClient = ProxyManager.Instance.GetProxy();
			}
			ThreadsManager.Instance.WaitHandle.WaitOne();
			try
			{
				using (HttpRequest httpRequest = new HttpRequest())
				{
					httpRequest.IgnoreProtocolErrors = true;
					httpRequest.ConnectTimeout = 15000;
					httpRequest.Proxy = proxyClient;
					httpRequest.KeepAlive = false;
					httpRequest.AllowAutoRedirect = false;
					httpRequest.UserAgent = "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)";
					string text = httpRequest.Get("http://" + prefix + this._domain + "/autodiscover/autodiscover.xml", null).ToString();
					if (string.IsNullOrEmpty(text))
					{
						return null;
					}
					if (text.Contains("<Protocol>"))
					{
						foreach (object obj in Regex.Matches(text, "<Protocol>(.*?)<\\/Protocol>", RegexOptions.Singleline))
						{
							Match match = (Match)obj;
							if (match.Success)
							{
								string value = match.Groups[1].Value;
								if (!value.Contains("<Type>SMTP</Type>"))
								{
									return new Server
									{
										Domain = this._domain,
										Hostname = value.Substring("<Server>", "</Server>", 0, StringComparison.Ordinal, null),
										Port = int.Parse(value.Substring("<Port>", "</Port>", 0, StringComparison.Ordinal, null)),
										Socket = (value.Contains("<SSL>on</SSL>") ? Hackus_Mail_Checker_Reforged.Models.Enums.SocketType.SSL : Hackus_Mail_Checker_Reforged.Models.Enums.SocketType.Plain),
										Protocol = (value.Contains("<Type>IMAP</Type>") ? Hackus_Mail_Checker_Reforged.Models.Enums.ProtocolType.IMAP : Hackus_Mail_Checker_Reforged.Models.Enums.ProtocolType.POP3)
									};
								}
							}
						}
					}
					if (string.IsNullOrEmpty(prefix))
					{
						return this.FindServerByAutoDiscover("autodiscover.", 0);
					}
				}
			}
			catch (ThreadAbortException)
			{
				throw;
			}
			catch (Exception ex)
			{
				if (ex is HttpException || ex is SocketException || ex is IOException || ex is ProxyException)
				{
					errors++;
					if (CheckerSettings.Instance.UseProxyToSearchServer && proxyClient != null && errors <= 2)
					{
						return this.FindServerByAutoDiscover(prefix, errors);
					}
					return null;
				}
			}
			return null;
		}

		// Token: 0x06000508 RID: 1288 RVA: 0x0001CA38 File Offset: 0x0001AC38
		public bool ConnectToServer(Server server)
		{
			MailClient mailClient = null;
			Hackus_Mail_Checker_Reforged.Models.Enums.ProtocolType protocol = server.Protocol;
			if (protocol == Hackus_Mail_Checker_Reforged.Models.Enums.ProtocolType.IMAP)
			{
				mailClient = new ImapClient();
			}
			else if (protocol == Hackus_Mail_Checker_Reforged.Models.Enums.ProtocolType.POP3)
			{
				mailClient = new Pop3Client();
			}
			mailClient.ReadWriteTimeout = 15000;
			mailClient.ConnectTimeout = 15000;
			int i = 0;
			while (i < 5)
			{
				TcpClient tcp = null;
				if (CheckerSettings.Instance.UseProxyToSearchServer)
				{
					ProxyClient proxy = ProxyManager.Instance.GetProxy();
					OperationResult operationResult = this.ConnectToProxy(server, proxy, out tcp);
					if (operationResult == OperationResult.Error)
					{
						i++;
						continue;
					}
					if (operationResult == OperationResult.HostNotFound)
					{
						return false;
					}
				}
				bool result;
				try
				{
					mailClient.Connect(server.Hostname, server.Port, server.Socket == Hackus_Mail_Checker_Reforged.Models.Enums.SocketType.SSL, tcp);
					mailClient.Dispose();
					result = true;
				}
				catch (ThreadAbortException)
				{
					throw;
				}
				catch
				{
					result = false;
				}
				return result;
			}
			return false;
		}

		// Token: 0x06000509 RID: 1289 RVA: 0x0001CB0C File Offset: 0x0001AD0C
		public OperationResult ConnectToProxy(Server server, ProxyClient proxyClient, out TcpClient tcpClient)
		{
			tcpClient = null;
			OperationResult result;
			try
			{
				tcpClient = proxyClient.CreateConnection(server.Hostname, server.Port, null);
				if (tcpClient != null && tcpClient.Connected)
				{
					result = OperationResult.Ok;
				}
				else
				{
					result = OperationResult.Error;
				}
			}
			catch (SocketException ex)
			{
				if (tcpClient != null && tcpClient.Connected)
				{
					tcpClient.DisposeObject();
				}
				if (ex.SocketErrorCode == SocketError.HostNotFound)
				{
					result = OperationResult.HostNotFound;
				}
				else
				{
					result = OperationResult.Error;
				}
			}
			catch
			{
				if (tcpClient != null && tcpClient.Connected)
				{
					tcpClient.DisposeObject();
				}
				result = OperationResult.Error;
			}
			return result;
		}

		// Token: 0x040002A7 RID: 679
		private object _contextLocker = new object();

		// Token: 0x040002A8 RID: 680
		public string _domain;

		// Token: 0x040002A9 RID: 681
		private int _inProcess;

		// Token: 0x040002AA RID: 682
		private ConcurrentQueue<Server> _serversToPing;

		// Token: 0x040002AB RID: 683
		private ConcurrentQueue<HostFinderContext.WebConfig> _webConfigs;

		// Token: 0x040002AC RID: 684
		public bool IsHandled;

		// Token: 0x040002AD RID: 685
		public bool IsStarted;

		// Token: 0x040002AE RID: 686
		public Server Result;

		// Token: 0x040002AF RID: 687
		public bool IsResultSaved;

		// Token: 0x040002B0 RID: 688
		private static readonly Dictionary<string, string> _domainCoincidences = new Dictionary<string, string>
		{
			{
				"yahoo.",
				"imap.mail.yahoo.com"
			},
			{
				"gmail.com",
				"imap.gmail.com"
			},
			{
				"google",
				"imap.gmail.com"
			},
			{
				"aol.",
				"imap.aol.com"
			},
			{
				"hotmail.",
				"outlook.office365.com"
			},
			{
				"live.com",
				"outlook.office365.com"
			},
			{
				"office365",
				"outlook.office365.com"
			},
			{
				"att.",
				"imap.mail.att.net"
			},
			{
				"wp.pl",
				"imap.wp.pl"
			},
			{
				"gmx.",
				"imap.gmx.com"
			},
			{
				"freenet.",
				"mx.freenet.de"
			},
			{
				"wanadoo.fr",
				"imap.orange.fr"
			},
			{
				"orange.fr",
				"imap.orange.fr"
			},
			{
				"yandex.",
				"imap.yandex.com"
			}
		};

		// Token: 0x040002B1 RID: 689
		private static readonly Dictionary<string, string> _recordCoincidences = new Dictionary<string, string>
		{
			{
				"mx-aol",
				"imap.aol.com"
			},
			{
				"yahoo",
				"imap.mail.yahoo.com"
			},
			{
				"google",
				"imap.gmail.com"
			},
			{
				"outlook",
				"outlook.office365.com"
			},
			{
				"secureserver",
				"imap.secureserver.net"
			},
			{
				"prodigy.net",
				"imap.mail.att.net"
			},
			{
				"wp.pl",
				"imap.wp.pl"
			},
			{
				"gmx.net",
				"imap.gmx.com"
			},
			{
				"web.de",
				"imap.web.de"
			},
			{
				"freenet.de",
				"mx.freenet.de"
			},
			{
				"orange.fr",
				"imap.orange.fr"
			},
			{
				"yandex",
				"imap.yandex.com"
			},
			{
				"kundenserver.de",
				"imap.1und1.de"
			},
			{
				"schlund.de",
				"imap.1und1.de"
			},
			{
				"1and1.co.uk",
				"imap.1und1.co.uk"
			},
			{
				"1and1.de",
				"imap.1und1.de"
			},
			{
				"1and1.com",
				"imap.1und1.com"
			},
			{
				"1and1.fr",
				"imap.1und1.fr"
			},
			{
				"1and1.es",
				"imap.1und1.es"
			},
			{
				"1and1.mx",
				"imap.1und1.mx"
			},
			{
				"1and1.at",
				"imap.1und1.at"
			},
			{
				"1and1.ca",
				"imap.1und1.ca"
			},
			{
				"1and1.ro",
				"imap.1und1.ro"
			},
			{
				"163.com",
				"imap.163.com"
			},
			{
				"nicmail.ru",
				"mail.nicmail.ru"
			},
			{
				"mail.dk",
				"imap.mail.dk"
			},
			{
				"lcn.com",
				"imap.lcn.com"
			},
			{
				"gosecure.net",
				"mail.gosecure.net"
			},
			{
				"zoho.com",
				"imap.zoho.com"
			},
			{
				"hostinger.com",
				"imap.hostinger.com"
			},
			{
				"everyone.net",
				"imap.everyone.net"
			},
			{
				"privateemail.com",
				"mail.privateemail.com"
			},
			{
				"netvigator.com",
				"imap.netvigator.com"
			},
			{
				"simply.com",
				"imap.simply.com"
			},
			{
				"transip.email",
				"imap.transip.email"
			},
			{
				"biz.rr.com",
				"mail.twc.com"
			},
			{
				"onlne.net",
				"imap.online.net"
			}
		};

		// Token: 0x040002B2 RID: 690
		private static List<string> _imapPrefixes = new List<string>
		{
			"imap.",
			"mail.",
			"mx.",
			"imap4.",
			"email."
		};

		// Token: 0x040002B3 RID: 691
		private static List<string> _pop3Prefixes = new List<string>
		{
			"pop.",
			"pop3.",
			"mail.",
			"email.",
			"mx."
		};

		// Token: 0x0200009A RID: 154
		private enum WebConfig
		{
			// Token: 0x040002B5 RID: 693
			None,
			// Token: 0x040002B6 RID: 694
			AutoConfig,
			// Token: 0x040002B7 RID: 695
			AutoDiscovery
		}
	}
}
