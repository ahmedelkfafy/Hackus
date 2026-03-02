using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Hackus_Mail_Checker_Reforged.Components.Viewer;
using Hackus_Mail_Checker_Reforged.Components.Viewer.Models;
using Hackus_Mail_Checker_Reforged.Models;
using Hackus_Mail_Checker_Reforged.Models.Enums;
using Hackus_Mail_Checker_Reforged.Services.Settings;
using Hackus_Mail_Checker_Reforged.UI.Models;
using HandyControl.Data;
using HandyControl.Tools;
using MailBee.Mime;
using MailBee.Pop3Mail;
using MailBee.Security;

namespace Hackus_Mail_Checker_Reforged.Components.Viewer.ViewModels.Tabs
{
	// Token: 0x02000180 RID: 384
	internal class Pop3TabViewModel : BindableObject, IDisposable
	{
		// Token: 0x06000B6B RID: 2923 RVA: 0x0003FBF4 File Offset: 0x0003DDF4
		public Pop3TabViewModel(Mailbox mailbox, Server server, Pop3 client)
		{
			this.Mailbox = mailbox;
			this.Server = server;
			this.Pop3 = client;
			this.Limit = ViewerSettings.Instance.PaginationLimit;
			this.ReconnectLimit = ViewerSettings.Instance.ReconnectLimit;
			this.Messages = new ObservableCollection<MailMessage>();
			this.Attachments = new ObservableCollection<Hackus_Mail_Checker_Reforged.Components.Viewer.Models.Attachment>();
			this.Pop3.MessageDownloaded += this.OnMessageDownloaded;
		}

		// Token: 0x17000230 RID: 560
		// (get) Token: 0x06000B6C RID: 2924 RVA: 0x0000CD64 File Offset: 0x0000AF64
		// (set) Token: 0x06000B6D RID: 2925 RVA: 0x0000CD6C File Offset: 0x0000AF6C
		public ObservableCollection<MailMessage> Messages
		{
			get
			{
				return this._messages;
			}
			set
			{
				this._messages = value;
				base.OnPropertyChanged(_Module_.smethod_6<string>(-1364324253));
			}
		}

		// Token: 0x17000231 RID: 561
		// (get) Token: 0x06000B6E RID: 2926 RVA: 0x0000CD85 File Offset: 0x0000AF85
		// (set) Token: 0x06000B6F RID: 2927 RVA: 0x0000CD8D File Offset: 0x0000AF8D
		public ObservableCollection<Hackus_Mail_Checker_Reforged.Components.Viewer.Models.Attachment> Attachments
		{
			get
			{
				return this._attachments;
			}
			set
			{
				this._attachments = value;
				base.OnPropertyChanged(_Module_.smethod_6<string>(-223475279));
			}
		}

		// Token: 0x17000232 RID: 562
		// (get) Token: 0x06000B70 RID: 2928 RVA: 0x0000CDA6 File Offset: 0x0000AFA6
		// (set) Token: 0x06000B71 RID: 2929 RVA: 0x0000CDAE File Offset: 0x0000AFAE
		public ObservableCollection<MailMessage> SelectedMessages
		{
			get
			{
				return this._selectedMessages;
			}
			set
			{
				this._selectedMessages = value;
				base.OnPropertyChanged(_Module_.smethod_4<string>(1261770006));
			}
		}

		// Token: 0x17000233 RID: 563
		// (get) Token: 0x06000B72 RID: 2930 RVA: 0x0000CDC7 File Offset: 0x0000AFC7
		// (set) Token: 0x06000B73 RID: 2931 RVA: 0x0000CDCF File Offset: 0x0000AFCF
		public Mailbox Mailbox
		{
			get
			{
				return this._mailbox;
			}
			set
			{
				this._mailbox = value;
				base.OnPropertyChanged(_Module_.smethod_5<string>(992838585));
			}
		}

		// Token: 0x17000234 RID: 564
		// (get) Token: 0x06000B74 RID: 2932 RVA: 0x0000CDE8 File Offset: 0x0000AFE8
		// (set) Token: 0x06000B75 RID: 2933 RVA: 0x0000CDF0 File Offset: 0x0000AFF0
		public Server Server
		{
			get
			{
				return this._server;
			}
			set
			{
				this._server = value;
				base.OnPropertyChanged(_Module_.smethod_6<string>(413798310));
			}
		}

		// Token: 0x17000235 RID: 565
		// (get) Token: 0x06000B76 RID: 2934 RVA: 0x0000CE09 File Offset: 0x0000B009
		// (set) Token: 0x06000B77 RID: 2935 RVA: 0x0000CE11 File Offset: 0x0000B011
		public Pop3 Pop3
		{
			get
			{
				return this._pop3;
			}
			set
			{
				this._pop3 = value;
				base.OnPropertyChanged(_Module_.smethod_4<string>(-1284541984));
			}
		}

		// Token: 0x17000236 RID: 566
		// (get) Token: 0x06000B78 RID: 2936 RVA: 0x0000CE2A File Offset: 0x0000B02A
		// (set) Token: 0x06000B79 RID: 2937 RVA: 0x0000CE32 File Offset: 0x0000B032
		public int Limit
		{
			get
			{
				return this._limit;
			}
			set
			{
				this._limit = value;
				base.OnPropertyChanged(_Module_.smethod_6<string>(-2104776208));
			}
		}

		// Token: 0x17000237 RID: 567
		// (get) Token: 0x06000B7A RID: 2938 RVA: 0x0000CE4B File Offset: 0x0000B04B
		// (set) Token: 0x06000B7B RID: 2939 RVA: 0x0000CE53 File Offset: 0x0000B053
		public int ReconnectLimit
		{
			get
			{
				return this._reconnectLimit;
			}
			set
			{
				this._reconnectLimit = value;
				base.OnPropertyChanged(_Module_.smethod_5<string>(367348827));
			}
		}

		// Token: 0x17000238 RID: 568
		// (get) Token: 0x06000B7C RID: 2940 RVA: 0x0000CE6C File Offset: 0x0000B06C
		// (set) Token: 0x06000B7D RID: 2941 RVA: 0x0000CE74 File Offset: 0x0000B074
		public int MaxPageCount
		{
			get
			{
				return this._maxPageCount;
			}
			set
			{
				this._maxPageCount = value;
				base.OnPropertyChanged(_Module_.smethod_2<string>(-1770412128));
			}
		}

		// Token: 0x17000239 RID: 569
		// (get) Token: 0x06000B7E RID: 2942 RVA: 0x0000CE8D File Offset: 0x0000B08D
		// (set) Token: 0x06000B7F RID: 2943 RVA: 0x0000CE95 File Offset: 0x0000B095
		public int PageIndex
		{
			get
			{
				return this._pageIndex;
			}
			set
			{
				this._pageIndex = value;
				base.OnPropertyChanged(_Module_.smethod_2<string>(1594299212));
				base.OnPropertyChanged(_Module_.smethod_6<string>(-333572785));
				base.OnPropertyChanged(_Module_.smethod_5<string>(734139158));
			}
		}

		// Token: 0x1700023A RID: 570
		// (get) Token: 0x06000B80 RID: 2944 RVA: 0x0000CECE File Offset: 0x0000B0CE
		// (set) Token: 0x06000B81 RID: 2945 RVA: 0x0000CED6 File Offset: 0x0000B0D6
		public bool IsPop3Busy
		{
			get
			{
				return this._isPop3Busy;
			}
			set
			{
				this._isPop3Busy = value;
				base.OnPropertyChanged(_Module_.smethod_3<string>(-1965364893));
			}
		}

		// Token: 0x1700023B RID: 571
		// (get) Token: 0x06000B82 RID: 2946 RVA: 0x0000CEEF File Offset: 0x0000B0EF
		// (set) Token: 0x06000B83 RID: 2947 RVA: 0x0000CEF7 File Offset: 0x0000B0F7
		public string Pop3OperationStatus
		{
			get
			{
				return this._pop3OperationStatus;
			}
			set
			{
				this._pop3OperationStatus = value;
				base.OnPropertyChanged(_Module_.smethod_3<string>(1767716119));
			}
		}

		// Token: 0x1700023C RID: 572
		// (get) Token: 0x06000B84 RID: 2948 RVA: 0x0000CF10 File Offset: 0x0000B110
		// (set) Token: 0x06000B85 RID: 2949 RVA: 0x0000CF18 File Offset: 0x0000B118
		public MailMessage Message
		{
			get
			{
				return this._message;
			}
			set
			{
				this._message = value;
				base.OnPropertyChanged(_Module_.smethod_6<string>(-1960491344));
			}
		}

		// Token: 0x1700023D RID: 573
		// (get) Token: 0x06000B86 RID: 2950 RVA: 0x0000CF31 File Offset: 0x0000B131
		// (set) Token: 0x06000B87 RID: 2951 RVA: 0x0000CF39 File Offset: 0x0000B139
		public bool ShowMessageBody
		{
			get
			{
				return this._showMessageBody;
			}
			set
			{
				this._showMessageBody = value;
				base.OnPropertyChanged(_Module_.smethod_4<string>(-1561064195));
			}
		}

		// Token: 0x1700023E RID: 574
		// (get) Token: 0x06000B88 RID: 2952 RVA: 0x0000CF52 File Offset: 0x0000B152
		// (set) Token: 0x06000B89 RID: 2953 RVA: 0x0000CF5A File Offset: 0x0000B15A
		public bool ShowAttachments
		{
			get
			{
				return this._showAttachments;
			}
			set
			{
				this._showAttachments = value;
				base.OnPropertyChanged(_Module_.smethod_4<string>(1252153368));
			}
		}

		// Token: 0x1700023F RID: 575
		// (get) Token: 0x06000B8A RID: 2954 RVA: 0x0000CF73 File Offset: 0x0000B173
		public bool CanMoveForward
		{
			get
			{
				return this.MaxPageCount > this.PageIndex;
			}
		}

		// Token: 0x17000240 RID: 576
		// (get) Token: 0x06000B8B RID: 2955 RVA: 0x0000CF83 File Offset: 0x0000B183
		public bool CanMoveBack
		{
			get
			{
				return this.PageIndex > 1;
			}
		}

		// Token: 0x06000B8C RID: 2956 RVA: 0x0003FC80 File Offset: 0x0003DE80
		private void OnMessageDownloaded(object sender, Pop3MessageDownloadedEventArgs e)
		{
			try
			{
				if (!string.IsNullOrEmpty(e.DownloadedMessage.From))
				{
					Application.Current.Dispatcher.Invoke(delegate()
					{
						MailMessage msg = e.DownloadedMessage;
						if (!this.Messages.Any(m => m.MessageId == msg.MessageId))
						{
							if (this.Messages != null)
								this.Messages.Add(msg);
						}
					});
				}
			}
			catch
			{
			}
		}

		// Token: 0x06000B8D RID: 2957 RVA: 0x0003FCF0 File Offset: 0x0003DEF0
		private async Task DownloadMailHeaders(int offset)
		{
			if (this.Pop3 == null) return;
			try
			{
				this.IsPop3Busy = true;
				this.Pop3OperationStatus = "Loading messages...";
				int totalMessages = await Task.Run(() =>
				{
					try { return this.Pop3.GetMessageCount(); }
					catch { return 0; }
				});
				this.MaxPageCount = Math.Max(1, (totalMessages + this.Limit - 1) / this.Limit);
				this.PageIndex = Math.Max(1, Math.Min(offset, this.MaxPageCount));
				int endIndex = totalMessages - (this.PageIndex - 1) * this.Limit;
				int startIndex = Math.Max(1, endIndex - this.Limit + 1);
				Application.Current.Dispatcher.Invoke(() => this.Messages.Clear());
				for (int i = startIndex; i <= endIndex; i++)
				{
					int index = i;
					await Task.Run(() =>
					{
						try { this.Pop3.DownloadEntireMessage(index, false); }
						catch { }
					});
				}
				this.Pop3OperationStatus = "Loaded " + this.Messages.Count + " messages";
			}
			catch (Exception ex)
			{
				this.Pop3OperationStatus = "Error: " + ex.Message;
			}
			finally
			{
				this.IsPop3Busy = false;
			}
		}

		// Token: 0x17000241 RID: 577
		// (get) Token: 0x06000B8E RID: 2958 RVA: 0x0003FD3C File Offset: 0x0003DF3C
		public RelayCommand UpdatePageCommand
		{
			get
			{
				if (this._updatePageCommand == null)
				{
					this._updatePageCommand = new RelayCommand(async (obj) =>
					{
						FunctionEventArgs<int> args = obj as FunctionEventArgs<int>;
						int page = (args != null) ? args.Info : 1;
						await this.DownloadMailHeaders(page);
					}, null);
				}
				return this._updatePageCommand;
			}
		}

		// Token: 0x06000B8F RID: 2959 RVA: 0x0003FD70 File Offset: 0x0003DF70
		private async Task<bool> EstablishConnection()
		{
			int reconnectCount = 0;
			while (reconnectCount < this.ReconnectLimit)
			{
				try
				{
					if (this.Pop3 != null)
					{
						this.Pop3.MessageDownloaded -= this.OnMessageDownloaded;
						try
						{
							await Task.Run(() =>
							{
								try { this.Pop3.Quit(); }
								catch { try { this.Pop3.Disconnect(); } catch { } }
							});
						}
						catch { }
					}
					Pop3 newPop3 = new Pop3();
					newPop3.SslMode = (this.Server.Socket == SocketType.SSL)
						? SslStartupMode.OnConnect
						: SslStartupMode.Manual;
					newPop3.Timeout = ViewerSettings.Instance.Timeout * 1000;
					bool connected = await Task.Run(() =>
					{
						try
						{
							newPop3.Connect(this.Server.Hostname, this.Server.Port);
							newPop3.Login(this.Mailbox.Address, this.Mailbox.Password);
							return true;
						}
						catch { return false; }
					});
					if (connected)
					{
						this.Pop3 = newPop3;
						this.Pop3.MessageDownloaded += this.OnMessageDownloaded;
						return true;
					}
				}
				catch { }
				reconnectCount++;
			}
			return false;
		}

		// Token: 0x17000242 RID: 578
		// (get) Token: 0x06000B90 RID: 2960 RVA: 0x0003FDB4 File Offset: 0x0003DFB4
		public RelayCommand InitializeCommand
		{
			get
			{
				if (this._initializeCommand == null)
				{
					this._initializeCommand = new RelayCommand(async (obj) =>
					{
						if (this._isInitialized) return;
						this._isInitialized = true;
						await this.LoadLast100MessagesAsync();
						this.InitializeKeepAlive();
					}, null);
				}
				return this._initializeCommand;
			}
		}

		// Token: 0x17000243 RID: 579
		// (get) Token: 0x06000B91 RID: 2961 RVA: 0x0003FDE8 File Offset: 0x0003DFE8
		public RelayCommand OpenMessagesListCommand
		{
			get
			{
				RelayCommand result;
				if ((result = this._openMessagesListCommand) == null)
				{
					result = (this._openMessagesListCommand = new RelayCommand(delegate(object obj)
					{
						this.ShowMessageBody = false;
					}, null));
				}
				return result;
			}
		}

		// Token: 0x17000244 RID: 580
		// (get) Token: 0x06000B92 RID: 2962 RVA: 0x0003FE1C File Offset: 0x0003E01C
		public RelayCommand GoForwardCommand
		{
			get
			{
				RelayCommand result;
				if ((result = this._goForwardCommand) == null)
				{
					result = (this._goForwardCommand = new RelayCommand(delegate(object obj)
					{
						this.UpdatePageCommand.Execute(new FunctionEventArgs<int>(this.PageIndex + 1));
					}, null));
				}
				return result;
			}
		}

		// Token: 0x17000245 RID: 581
		// (get) Token: 0x06000B93 RID: 2963 RVA: 0x0003FE50 File Offset: 0x0003E050
		public RelayCommand GoBackCommand
		{
			get
			{
				RelayCommand result;
				if ((result = this._goBackCommand) == null)
				{
					result = (this._goBackCommand = new RelayCommand(delegate(object obj)
					{
						this.UpdatePageCommand.Execute(new FunctionEventArgs<int>(this.PageIndex - 1));
					}, null));
				}
				return result;
			}
		}

		// Token: 0x17000246 RID: 582
		// (get) Token: 0x06000B94 RID: 2964 RVA: 0x0003FE84 File Offset: 0x0003E084
		public RelayCommand OpenMessageCommand
		{
			get
			{
				if (this._openMessageCommand == null)
				{
					this._openMessageCommand = new RelayCommand(async (obj) =>
					{
						MailMessage msg = obj as MailMessage;
						if (msg == null) return;
						// Message body is already downloaded (headersOnly=false in LoadLast100MessagesAsync)
						this.Message = msg;
						this.ShowMessageBody = true;
						await Task.CompletedTask;
					}, null);
				}
				return this._openMessageCommand;
			}
		}

		// Token: 0x17000247 RID: 583
		// (get) Token: 0x06000B95 RID: 2965 RVA: 0x0003FEB8 File Offset: 0x0003E0B8
		public RelayCommand SwitchAttachmentsModeCommand
		{
			get
			{
				RelayCommand result;
				if ((result = this._switchAttachmentsModeCommand) == null)
				{
					result = (this._switchAttachmentsModeCommand = new RelayCommand(delegate(object obj)
					{
						if (!this.ShowAttachments)
						{
							this.Attachments.Clear();
							foreach (object obj2 in this.Message.Attachments)
							{
								MailBee.Mime.Attachment attachment = (MailBee.Mime.Attachment)obj2;
								this.Attachments.Add(new Hackus_Mail_Checker_Reforged.Components.Viewer.Models.Attachment(attachment));
							}
						}
						this.ShowAttachments = !this.ShowAttachments;
					}, null));
				}
				return result;
			}
		}

		// Token: 0x17000248 RID: 584
		// (get) Token: 0x06000B96 RID: 2966 RVA: 0x0003FEEC File Offset: 0x0003E0EC
		public RelayCommand DownloadAttachmentCommand
		{
			get
			{
				if (this._downloadAttachmentCommand == null)
				{
					this._downloadAttachmentCommand = new RelayCommand(async (obj) =>
					{
						Hackus_Mail_Checker_Reforged.Components.Viewer.Models.Attachment attachment = obj as Hackus_Mail_Checker_Reforged.Components.Viewer.Models.Attachment;
						if (attachment?.InnerAttachment == null) return;
						try
						{
							string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
							string filePath = Path.Combine(downloadsPath, attachment.Filename ?? "attachment");
							await Task.Run(() => attachment.InnerAttachment.Save(filePath, true));
							attachment.IsSaved = true;
						}
						catch { }
					}, null);
				}
				return this._downloadAttachmentCommand;
			}
		}

		// Token: 0x17000249 RID: 585
		// (get) Token: 0x06000B97 RID: 2967 RVA: 0x0003FF34 File Offset: 0x0003E134
		public RelayCommand DownloadAttachmentByPathCommand
		{
			get
			{
				if (this._downloadAttachmentByPathCommand == null)
				{
					this._downloadAttachmentByPathCommand = new RelayCommand(async (obj) =>
					{
						Hackus_Mail_Checker_Reforged.Components.Viewer.Models.Attachment attachment = obj as Hackus_Mail_Checker_Reforged.Components.Viewer.Models.Attachment;
						if (attachment?.InnerAttachment == null) return;
						try
						{
							var dialog = new Microsoft.Win32.SaveFileDialog();
							dialog.FileName = attachment.Filename ?? "attachment";
							if (dialog.ShowDialog() == true)
							{
								await Task.Run(() => attachment.InnerAttachment.Save(dialog.FileName, true));
								attachment.IsSaved = true;
							}
						}
						catch { }
					}, null);
				}
				return this._downloadAttachmentByPathCommand;
			}
		}

		// Token: 0x1700024A RID: 586
		// (get) Token: 0x06000B98 RID: 2968 RVA: 0x0003FF7C File Offset: 0x0003E17C
		public RelayCommand DisconnectCommand
		{
			get
			{
				if (this._disconnectCommand == null)
				{
					this._disconnectCommand = new RelayCommand(async (obj) =>
					{
						try
						{
							if (this.Pop3 != null && this.Pop3.IsConnected)
							{
								await Task.Run(() =>
								{
									try { this.Pop3.Quit(); }
									catch { try { this.Pop3.Disconnect(); } catch { } }
								});
							}
						}
						finally
						{
							ViewerController.Instance.CloseTab(this);
						}
					}, null);
				}
				return this._disconnectCommand;
			}
		}

		// Token: 0x1700024B RID: 587
		// (get) Token: 0x06000B99 RID: 2969 RVA: 0x0003FFB0 File Offset: 0x0003E1B0
		public RelayCommand ChangeTranslationFromLanguageCommand
		{
			get
			{
				RelayCommand result;
				if ((result = this._changeTranslationFromLanguageCommand) == null)
				{
					result = (this._changeTranslationFromLanguageCommand = new RelayCommand(delegate(object obj)
					{
						ComboBox comboBox = (obj as SelectionChangedEventArgs).Source as ComboBox;
						ComboBoxItem comboBoxItem = ((comboBox != null) ? comboBox.SelectedItem : null) as ComboBoxItem;
						string value2 = ((comboBoxItem != null) ? comboBoxItem.Tag : null) as string;
						TranslationLanguage translationFromLanguage = (TranslationLanguage)Enum.Parse(typeof(TranslationLanguage), value2);
						this._translationFromLanguage = translationFromLanguage;
						ViewerSettings.Instance.TranslationFromLanguage = translationFromLanguage;
					}, null));
				}
				return result;
			}
		}

		// Token: 0x1700024C RID: 588
		// (get) Token: 0x06000B9A RID: 2970 RVA: 0x0003FFE4 File Offset: 0x0003E1E4
		public RelayCommand ChangeTranslationToLanguageCommand
		{
			get
			{
				RelayCommand result;
				if ((result = this._changeTranslationToLanguageCommand) == null)
				{
					result = (this._changeTranslationToLanguageCommand = new RelayCommand(delegate(object obj)
					{
						ComboBox comboBox = (obj as SelectionChangedEventArgs).Source as ComboBox;
						ComboBoxItem comboBoxItem = ((comboBox != null) ? comboBox.SelectedItem : null) as ComboBoxItem;
						string value2 = ((comboBoxItem != null) ? comboBoxItem.Tag : null) as string;
						TranslationLanguage translationToLanguage = (TranslationLanguage)Enum.Parse(typeof(TranslationLanguage), value2);
						this._translationToLanguage = translationToLanguage;
						ViewerSettings.Instance.TranslationToLanguage = translationToLanguage;
					}, null));
				}
				return result;
			}
		}

		// Token: 0x1700024D RID: 589
		// (get) Token: 0x06000B9B RID: 2971 RVA: 0x00040018 File Offset: 0x0003E218
		public RelayCommand TranslateCommand
		{
			get
			{
				if (this._translateCommand == null)
				{
					this._translateCommand = new RelayCommand(async (obj) =>
					{
						await Task.CompletedTask;
					}, null);
				}
				return this._translateCommand;
			}
		}

		// Token: 0x06000B9C RID: 2972 RVA: 0x0000CF8E File Offset: 0x0000B18E
		public void Dispose()
		{
			if (!this._isDisposed)
			{
				_keepAliveTimer?.Stop();
				_keepAliveTimer = null;
				this.DisconnectCommand.Execute(null);
				this._isDisposed = true;
			}
			GC.SuppressFinalize(this);
		}

		private DispatcherTimer _keepAliveTimer;

		private void InitializeKeepAlive()
		{
			_keepAliveTimer = new DispatcherTimer();
			_keepAliveTimer.Interval = TimeSpan.FromMinutes(2);
			_keepAliveTimer.Tick += async (s, e) => await KeepAliveAsync();
			_keepAliveTimer.Start();
		}

		private async Task KeepAliveAsync()
		{
			try
			{
				if (this.Pop3 != null && this.Pop3.IsConnected)
					await Task.Run(() => this.Pop3.Noop());
			}
			catch
			{
				await ReconnectAsync();
			}
		}

		private async Task ReconnectAsync()
		{
			bool success = await this.EstablishConnection();
			if (!success)
			{
				_keepAliveTimer?.Stop();
			}
		}

		private async Task LoadLast100MessagesAsync()
		{
			try
			{
				this.IsPop3Busy = true;
				this.Pop3OperationStatus = "Loading last 100 messages...";
				int totalMessages = await Task.Run(() =>
				{
					try { return this.Pop3.GetMessageCount(); }
					catch { return 0; }
				});
				int startIndex = Math.Max(1, totalMessages - 99);
				Application.Current.Dispatcher.Invoke(() => this.Messages.Clear());
				for (int i = startIndex; i <= totalMessages; i++)
				{
					int index = i;
					await Task.Run(() =>
					{
						try { this.Pop3.DownloadEntireMessage(index, false); }
						catch { }
					});
				}
				this.Pop3OperationStatus = "Loaded " + this.Messages.Count + " messages";
			}
			catch (Exception ex)
			{
				this.Pop3OperationStatus = "Error: " + ex.Message;
			}
			finally
			{
				this.IsPop3Busy = false;
			}
		}

		// Token: 0x06000B9D RID: 2973 RVA: 0x00006BF6 File Offset: 0x00004DF6
		public string Resource(string key)
		{
			return ResourceHelper.GetResource<string>(key);
		}

		// Token: 0x04000600 RID: 1536
		private bool _isInitialized;

		// Token: 0x04000601 RID: 1537
		private bool _isDisposed;

		// Token: 0x04000602 RID: 1538
		private TranslationLanguage _translationFromLanguage;

		// Token: 0x04000603 RID: 1539
		private TranslationLanguage _translationToLanguage;

		// Token: 0x04000604 RID: 1540
		private ObservableCollection<MailMessage> _messages;

		// Token: 0x04000605 RID: 1541
		private ObservableCollection<Hackus_Mail_Checker_Reforged.Components.Viewer.Models.Attachment> _attachments;

		// Token: 0x04000606 RID: 1542
		private ObservableCollection<MailMessage> _selectedMessages;

		// Token: 0x04000607 RID: 1543
		private Mailbox _mailbox;

		// Token: 0x04000608 RID: 1544
		private Server _server;

		// Token: 0x04000609 RID: 1545
		private Pop3 _pop3;

		// Token: 0x0400060A RID: 1546
		private int _limit = 50;

		// Token: 0x0400060B RID: 1547
		private int _reconnectLimit = 1;

		// Token: 0x0400060C RID: 1548
		private int _maxPageCount = 1;

		// Token: 0x0400060D RID: 1549
		private int _pageIndex;

		// Token: 0x0400060E RID: 1550
		private bool _isPop3Busy;

		// Token: 0x0400060F RID: 1551
		private string _pop3OperationStatus;

		// Token: 0x04000610 RID: 1552
		private MailMessage _message;

		// Token: 0x04000611 RID: 1553
		private bool _showMessageBody;

		// Token: 0x04000612 RID: 1554
		private bool _showAttachments;

		// Token: 0x04000613 RID: 1555
		private RelayCommand _updatePageCommand;

		// Token: 0x04000614 RID: 1556
		private RelayCommand _initializeCommand;

		// Token: 0x04000615 RID: 1557
		private RelayCommand _openMessagesListCommand;

		// Token: 0x04000616 RID: 1558
		private RelayCommand _goForwardCommand;

		// Token: 0x04000617 RID: 1559
		private RelayCommand _goBackCommand;

		// Token: 0x04000618 RID: 1560
		private RelayCommand _openMessageCommand;

		// Token: 0x04000619 RID: 1561
		private RelayCommand _switchAttachmentsModeCommand;

		// Token: 0x0400061A RID: 1562
		private RelayCommand _downloadAttachmentCommand;

		// Token: 0x0400061B RID: 1563
		private RelayCommand _downloadAttachmentByPathCommand;

		// Token: 0x0400061C RID: 1564
		private RelayCommand _disconnectCommand;

		// Token: 0x0400061D RID: 1565
		private RelayCommand _changeTranslationFromLanguageCommand;

		// Token: 0x0400061E RID: 1566
		private RelayCommand _changeTranslationToLanguageCommand;

		// Token: 0x0400061F RID: 1567
		private RelayCommand _translateCommand;
	}
}
