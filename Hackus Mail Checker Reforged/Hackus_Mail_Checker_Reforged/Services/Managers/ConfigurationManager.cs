using System;
using System.Data.SQLite;
using System.Linq;
using System.Xml;
using DnsClient;
using DnsClient.Protocol;
using Hackus_Mail_Checker_Reforged.Models;
using Hackus_Mail_Checker_Reforged.Models.Enums;
using Hackus_Mail_Checker_Reforged.Net.Mail;
using Hackus_Mail_Checker_Reforged.Services.Settings;
using Hackus_Mail_Checker_Reforged.UI.Models;

namespace Hackus_Mail_Checker_Reforged.Services.Managers
{
	// Token: 0x0200006C RID: 108
	internal class ConfigurationManager : BindableObject
	{
		// Token: 0x060003C0 RID: 960 RVA: 0x00006C91 File Offset: 0x00004E91
		private ConfigurationManager()
		{
		}

		// Token: 0x170000FE RID: 254
		// (get) Token: 0x060003C1 RID: 961 RVA: 0x00008D0B File Offset: 0x00006F0B
		public static ConfigurationManager Instance
		{
			get
			{
				ConfigurationManager result;
				if ((result = ConfigurationManager._instance) == null)
				{
					result = (ConfigurationManager._instance = new ConfigurationManager());
				}
				return result;
			}
		}

		/// <summary>
		/// Initializes the multi-database architecture with separate IMAP and POP3 databases.
		/// </summary>
		/// <param name="imapDbPath">Path to imap.db (provided by user).</param>
		/// <param name="pop3DbPath">Path to pop3.db (auto-created and populated during checking).</param>
		public void Initialize(string imapDbPath, string pop3DbPath)
		{
			string imapConnectionString = string.Format("Data Source={0};Version=3;", imapDbPath);
			ImapConfiguration = new SqlConfiguration(imapConnectionString, imapConnectionString);

			string pop3ConnectionString = string.Format("Data Source={0};Version=3;", pop3DbPath);
			Pop3Configuration = new SqlConfiguration(pop3ConnectionString, pop3ConnectionString);

			// Point Configuration to ImapConfiguration for backward compatibility
			Configuration = ImapConfiguration;

			EnsurePop3TableExists(pop3DbPath);
		}

		private void EnsurePop3TableExists(string pop3DbPath)
		{
			try
			{
				string connectionString = string.Format("Data Source={0};Version=3;", pop3DbPath);
				using (var conn = new SQLiteConnection(connectionString))
				{
					conn.Open();
					var cmd = conn.CreateCommand();
					cmd.CommandText =
						"CREATE TABLE IF NOT EXISTS POP3 (" +
						"Domain TEXT NOT NULL UNIQUE," +
						"Server TEXT NOT NULL," +
						"Port INTEGER NOT NULL DEFAULT 995," +
						"Socket INTEGER NOT NULL DEFAULT 0" +
						")";
					cmd.ExecuteNonQuery();
				}
			}
			catch
			{
			}
		}

		/// <summary>
		/// Finds a server for the given domain and protocol, routing to the appropriate database.
		/// </summary>
		public Server Find(string domain, ProtocolType protocol)
		{
			if (protocol == ProtocolType.IMAP && ImapConfiguration != null)
			{
				Server result = ImapConfiguration.Find(domain, ProtocolType.IMAP);
				if (result != null) return result;
				result = ImapConfiguration.FindInDatabase(domain, ProtocolType.IMAP);
				if (result != null)
				{
					ImapConfiguration.Add(result);
					return result;
				}
			}
			else if (protocol == ProtocolType.POP3 && Pop3Configuration != null)
			{
				Server result = Pop3Configuration.Find(domain, ProtocolType.POP3);
				if (result != null) return result;
				result = Pop3Configuration.FindInDatabase(domain, ProtocolType.POP3);
				if (result != null)
				{
					Pop3Configuration.Add(result);
					return result;
				}
			}

			// Fallback to single Configuration (backward compatibility)
			if (Configuration != null)
				return Configuration.Find(domain, protocol);

			return null;
		}

		/// <summary>
		/// Finds a POP3 server for the given domain, running auto-discovery if not already known.
		/// Discovered servers are saved to pop3.db for future use.
		/// </summary>
		public Server FindOrDiscoverPOP3(string domain)
		{
			// 1. Try in-memory cache
			if (Pop3Configuration != null)
			{
				Server pop3Server = Pop3Configuration.Find(domain, ProtocolType.POP3);
				if (pop3Server != null)
					return pop3Server;

				// 2. Try database
				pop3Server = Pop3Configuration.FindInDatabase(domain, ProtocolType.POP3);
				if (pop3Server != null)
				{
					Pop3Configuration.Add(pop3Server); // Update in-memory cache
					return pop3Server;
				}
			}

			// 3. Not found - try auto-discovery
			if (!CheckerSettings.Instance.EnableAutoDiscovery)
				return null;

			Server imapServer = null;
			if (ImapConfiguration != null)
			{
				imapServer = ImapConfiguration.Find(domain, ProtocolType.IMAP);
				if (imapServer == null)
					imapServer = ImapConfiguration.FindInDatabase(domain, ProtocolType.IMAP);
			}

			Server discovered = AutoDiscoverPOP3(domain, imapServer);

			if (discovered != null && Pop3Configuration != null)
			{
				// Save to pop3.db for future use
				Pop3Configuration.Add(discovered);
			}

			return discovered;
		}

		private Server AutoDiscoverPOP3(string domain, Server imapServer)
		{
			// Try in order: ISPDB → AutoDiscover → AutoConfig → Well-Known → Heuristics → MX Records
			Server discovered = TryISPDB(domain, ProtocolType.POP3);
			if (discovered != null) return discovered;

			discovered = TryAutoDiscoverSubdomain(domain, ProtocolType.POP3);
			if (discovered != null) return discovered;

			discovered = TryAutoDiscoverRoot(domain, ProtocolType.POP3);
			if (discovered != null) return discovered;

			discovered = TryAutoConfigHTTPS(domain, ProtocolType.POP3);
			if (discovered != null) return discovered;

			discovered = TryAutoConfigHTTP(domain, ProtocolType.POP3);
			if (discovered != null) return discovered;

			discovered = TryWellKnown(domain, ProtocolType.POP3);
			if (discovered != null) return discovered;

			if (imapServer != null)
			{
				discovered = TryHeuristics(domain, imapServer, ProtocolType.POP3);
				if (discovered != null) return discovered;
			}

			discovered = TryMXRecords(domain, ProtocolType.POP3);
			return discovered;
		}

		private Server TryISPDB(string domain, ProtocolType protocol)
		{
			try
			{
				string url = string.Format("https://autoconfig.thunderbird.net/v1.1/{0}", domain);
				string xml;
				using (var webClient = new System.Net.WebClient())
				{
					webClient.Headers.Add("User-Agent", UserAgent);
					xml = webClient.DownloadString(url);
				}

				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.LoadXml(xml);

				string typeFilter = protocol == ProtocolType.POP3 ? "pop3" : "imap";
				XmlNodeList serverNodes = xmlDoc.GetElementsByTagName("incomingServer");

				foreach (XmlNode node in serverNodes)
				{
					string serverType = node.Attributes?["type"]?.Value;
					if (!string.Equals(serverType, typeFilter, StringComparison.OrdinalIgnoreCase))
						continue;

					string hostname = node["hostname"]?.InnerText;
					string portStr = node["port"]?.InnerText;
					string socketTypeStr = node["socketType"]?.InnerText ?? "SSL";

					if (string.IsNullOrEmpty(hostname)) continue;

					int port;
					if (!int.TryParse(portStr, out port))
						port = protocol == ProtocolType.POP3 ? 995 : 993;

					SocketType socket = string.Equals(socketTypeStr, "SSL", StringComparison.OrdinalIgnoreCase)
						? SocketType.SSL
						: SocketType.Plain;

					if (TestConnection(hostname, port, protocol, socket))
						return new Server(domain, hostname, port, protocol, socket);
				}
			}
			catch
			{
			}
			return null;
		}

		private static string BuildAutoDiscoverPayload(string domain)
		{
			return string.Format(
				"<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
				"<Autodiscover xmlns=\"http://schemas.microsoft.com/exchange/autodiscover/outlook/requestschema/2006\">" +
				"<Request>" +
				"<EMailAddress>user@{0}</EMailAddress>" +
				"<AcceptableResponseSchema>http://schemas.microsoft.com/exchange/autodiscover/outlook/responseschema/2006a</AcceptableResponseSchema>" +
				"</Request>" +
				"</Autodiscover>", domain);
		}

		private Server TryAutoDiscoverSubdomain(string domain, ProtocolType protocol)
		{
			try
			{
				string url = string.Format("https://autodiscover.{0}/autodiscover/autodiscover.xml", domain);
				string response;
				using (var webClient = new System.Net.WebClient())
				{
					webClient.Headers.Add("User-Agent", UserAgent);
					webClient.Headers.Add("Content-Type", "text/xml; charset=utf-8");
					response = webClient.UploadString(url, BuildAutoDiscoverPayload(domain));
				}

				return ParseAutoDiscoverResponse(domain, response, protocol);
			}
			catch
			{
			}
			return null;
		}

		private Server TryAutoDiscoverRoot(string domain, ProtocolType protocol)
		{
			try
			{
				string url = string.Format("https://{0}/autodiscover/autodiscover.xml", domain);
				string response;
				using (var webClient = new System.Net.WebClient())
				{
					webClient.Headers.Add("User-Agent", UserAgent);
					webClient.Headers.Add("Content-Type", "text/xml; charset=utf-8");
					response = webClient.UploadString(url, BuildAutoDiscoverPayload(domain));
				}

				return ParseAutoDiscoverResponse(domain, response, protocol);
			}
			catch
			{
			}
			return null;
		}

		private Server ParseAutoDiscoverResponse(string domain, string xml, ProtocolType protocol)
		{
			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.LoadXml(xml);

			string typeFilter = protocol == ProtocolType.POP3 ? "POP3" : "IMAP";
			XmlNodeList protocolNodes = xmlDoc.GetElementsByTagName("Protocol");

			foreach (XmlNode node in protocolNodes)
			{
				string serverType = node["Type"]?.InnerText;
				if (!string.Equals(serverType, typeFilter, StringComparison.OrdinalIgnoreCase))
					continue;

				string hostname = node["Server"]?.InnerText;
				string portStr = node["Port"]?.InnerText;
				string sslStr = node["SSL"]?.InnerText ?? "on";

				if (string.IsNullOrEmpty(hostname)) continue;

				int port;
				if (!int.TryParse(portStr, out port))
					port = protocol == ProtocolType.POP3 ? 995 : 993;

				SocketType socket = string.Equals(sslStr, "on", StringComparison.OrdinalIgnoreCase)
					? SocketType.SSL
					: SocketType.Plain;

				if (TestConnection(hostname, port, protocol, socket))
					return new Server(domain, hostname, port, protocol, socket);
			}

			return null;
		}

		private Server TryAutoConfigHTTPS(string domain, ProtocolType protocol)
		{
			try
			{
				string url = string.Format("https://autoconfig.{0}/mail/config-v1.1.xml", domain);
				string xml;
				using (var webClient = new System.Net.WebClient())
				{
					webClient.Headers.Add("User-Agent", UserAgent);
					xml = webClient.DownloadString(url);
				}

				return ParseAutoConfigResponse(domain, xml, protocol);
			}
			catch
			{
			}
			return null;
		}

		private Server TryAutoConfigHTTP(string domain, ProtocolType protocol)
		{
			try
			{
				string url = string.Format("http://autoconfig.{0}/mail/config-v1.1.xml", domain);
				string xml;
				using (var webClient = new System.Net.WebClient())
				{
					webClient.Headers.Add("User-Agent", UserAgent);
					xml = webClient.DownloadString(url);
				}

				return ParseAutoConfigResponse(domain, xml, protocol);
			}
			catch
			{
			}
			return null;
		}

		private Server TryWellKnown(string domain, ProtocolType protocol)
		{
			try
			{
				string url = string.Format("https://{0}/.well-known/autoconfig/mail/config-v1.1.xml", domain);
				string xml;
				using (var webClient = new System.Net.WebClient())
				{
					webClient.Headers.Add("User-Agent", UserAgent);
					xml = webClient.DownloadString(url);
				}

				return ParseAutoConfigResponse(domain, xml, protocol);
			}
			catch
			{
			}
			return null;
		}

		private Server ParseAutoConfigResponse(string domain, string xml, ProtocolType protocol)
		{
			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.LoadXml(xml);

			string typeFilter = protocol == ProtocolType.POP3 ? "pop3" : "imap";
			XmlNodeList serverNodes = xmlDoc.GetElementsByTagName("incomingServer");

			foreach (XmlNode node in serverNodes)
			{
				string serverType = node.Attributes?["type"]?.Value;
				if (!string.Equals(serverType, typeFilter, StringComparison.OrdinalIgnoreCase))
					continue;

				string hostname = node["hostname"]?.InnerText;
				string portStr = node["port"]?.InnerText;
				string socketTypeStr = node["socketType"]?.InnerText ?? "SSL";

				if (string.IsNullOrEmpty(hostname)) continue;

				int port;
				if (!int.TryParse(portStr, out port))
					port = protocol == ProtocolType.POP3 ? 995 : 993;

				SocketType socket = string.Equals(socketTypeStr, "SSL", StringComparison.OrdinalIgnoreCase)
					? SocketType.SSL
					: SocketType.Plain;

				if (TestConnection(hostname, port, protocol, socket))
					return new Server(domain, hostname, port, protocol, socket);
			}

			return null;
		}

		private Server TryHeuristics(string domain, Server imapServer, ProtocolType protocol)
		{
			// Extract base domain from IMAP hostname (e.g., "imap.gmail.com" → "gmail.com")
			string baseDomain = domain;
			if (imapServer != null)
			{
				string imapHost = imapServer.Hostname.ToLower();
				string[] knownPrefixes = new[] { "imap.", "mail.", "smtp.", "pop.", "pop3." };
				foreach (string prefix in knownPrefixes)
				{
					if (imapHost.StartsWith(prefix))
					{
						baseDomain = imapHost.Substring(prefix.Length);
						break;
					}
				}
			}

			string[] candidatePrefixes = new[] { "pop.", "pop3.", "mail." };
			int[] ports = new[] { 995, 110 };
			SocketType[] sockets = new[] { SocketType.SSL, SocketType.Plain };
			int pairCount = Math.Min(ports.Length, sockets.Length);

			foreach (string prefix in candidatePrefixes)
			{
				string hostname = prefix + baseDomain;
				for (int i = 0; i < pairCount; i++)
				{
					if (TestConnection(hostname, ports[i], protocol, sockets[i]))
						return new Server(domain, hostname, ports[i], protocol, sockets[i]);
				}
			}

			return null;
		}

		private Server TryMXRecords(string domain, ProtocolType protocol)
		{
			try
			{
				var lookupClient = new LookupClient { UseCache = false, ThrowDnsErrors = false };
				var result = lookupClient.Query(domain, QueryType.MX);
				var mxRecord = RecordCollectionExtension.MxRecords(result.Answers)
					.OrderBy(r => r.Preference)
					.FirstOrDefault();

				if (mxRecord == null) return null;

				// Use implicit conversion consistent with HostFinder.GetMx()
				string mx = mxRecord.Exchange;
				if (string.IsNullOrEmpty(mx)) return null;
				mx = mx.TrimEnd('.');

				// Extract root domain from MX record (e.g., "aspmx.l.google.com" → "google.com")
				string[] parts = mx.Split('.');
				string rootDomain = parts.Length >= 2
					? parts[parts.Length - 2] + "." + parts[parts.Length - 1]
					: mx;

				string[] prefixes = new[] { "pop.", "pop3.", "mail." };
				int[] ports = new[] { 995, 110 };
				SocketType[] sockets = new[] { SocketType.SSL, SocketType.Plain };
				int pairCount = Math.Min(ports.Length, sockets.Length);

				foreach (string prefix in prefixes)
				{
					string hostname = prefix + rootDomain;
					for (int i = 0; i < pairCount; i++)
					{
						if (TestConnection(hostname, ports[i], protocol, sockets[i]))
							return new Server(domain, hostname, ports[i], protocol, sockets[i]);
					}
				}
			}
			catch
			{
			}
			return null;
		}

		private bool TestConnection(string hostname, int port, ProtocolType protocol, SocketType socket)
		{
			Client client = null;
			try
			{
				if (protocol == ProtocolType.IMAP)
					client = new Imap();
				else
					client = new Pop3();

				client.ServerTimeout = 3000;
				client.Connect(hostname, port, socket == SocketType.SSL, null);
				return true;
			}
			catch
			{
			}
			finally
			{
				try { client?.Close(); } catch { }
			}
			return false;
		}

		// Token: 0x04000219 RID: 537
		private static ConfigurationManager _instance;

		private const string UserAgent = "HackusMailChecker/1.0";

		// Token: 0x0400021A RID: 538
		public SqlConfiguration Configuration;

		// Token: 0x0400021B RID: 539
		public SqlConfiguration ImapConfiguration;

		// Token: 0x0400021C RID: 540
		public SqlConfiguration Pop3Configuration;
	}
}
