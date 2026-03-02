using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
using MailBee.ImapMail;
using MailBee.Mime;
using MailBee.Security;

namespace Hackus_Mail_Checker_Reforged.Components.Viewer.ViewModels.Tabs
{
	// Token: 0x0200016C RID: 364
	internal class ImapTabViewModel : BindableObject, IDisposable
	{
		// Token: 0x06000A91 RID: 2705 RVA: 0x0003CC98 File Offset: 0x0003AE98
		public ImapTabViewModel(Mailbox mailbox, Server server, Imap client)
		{
			this.Mailbox = mailbox;
			this.Server = server;
			this.Imap = client;
			this.Limit = ViewerSettings.Instance.PaginationLimit;
			this.ReconnectLimit = ViewerSettings.Instance.ReconnectLimit;
			this.Folders = new ObservableCollection<ImapFolder>();
			this.Messages = new ObservableCollection<Envelope>();
			this.SelectedMessages = new ObservableCollection<Envelope>();
			this.Attachments = new ObservableCollection<Hackus_Mail_Checker_Reforged.Components.Viewer.Models.Attachment>();
			this.SearchQuery = new SearchQuery();
			this.Imap.EnvelopeDownloaded += this.OnEnvelopeDownloaded;
			this._translationFromLanguage = ViewerSettings.Instance.TranslationFromLanguage;
			this._translationToLanguage = ViewerSettings.Instance.TranslationToLanguage;
		}

		// Token: 0x17000203 RID: 515
		// (get) Token: 0x06000A92 RID: 2706 RVA: 0x0000C6EC File Offset: 0x0000A8EC
		// (set) Token: 0x06000A93 RID: 2707 RVA: 0x0000C6F4 File Offset: 0x0000A8F4
		public ObservableCollection<ImapFolder> Folders
		{
			get
			{
				return this._folders;
			}
			set
			{
				this._folders = value;
				base.OnPropertyChanged(nameof(Folders));
			}
		}

		// Token: 0x17000204 RID: 516
		// (get) Token: 0x06000A94 RID: 2708 RVA: 0x0000C70D File Offset: 0x0000A90D
		// (set) Token: 0x06000A95 RID: 2709 RVA: 0x0000C715 File Offset: 0x0000A915
		public ObservableCollection<Envelope> Messages
		{
			get
			{
				return this._messages;
			}
			set
			{
				this._messages = value;
				base.OnPropertyChanged(nameof(Messages));
			}
		}

		// Token: 0x17000205 RID: 517
		// (get) Token: 0x06000A96 RID: 2710 RVA: 0x0000C72E File Offset: 0x0000A92E
		// (set) Token: 0x06000A97 RID: 2711 RVA: 0x0000C736 File Offset: 0x0000A936
		public ObservableCollection<Envelope> SelectedMessages
		{
			get
			{
				return this._selectedMessages;
			}
			set
			{
				this._selectedMessages = value;
				base.OnPropertyChanged(nameof(SelectedMessages));
			}
		}

		// Token: 0x17000206 RID: 518
		// (get) Token: 0x06000A98 RID: 2712 RVA: 0x0000C74F File Offset: 0x0000A94F
		// (set) Token: 0x06000A99 RID: 2713 RVA: 0x0000C757 File Offset: 0x0000A957
		public UidCollection SearchedUids
		{
			get
			{
				return this._searchedUids;
			}
			set
			{
				this._searchedUids = value;
				base.OnPropertyChanged(nameof(SearchedUids));
			}
		}

		// Token: 0x17000207 RID: 519
		// (get) Token: 0x06000A9A RID: 2714 RVA: 0x0000C770 File Offset: 0x0000A970
		// (set) Token: 0x06000A9B RID: 2715 RVA: 0x0000C778 File Offset: 0x0000A978
		public Mailbox Mailbox
		{
			get
			{
				return this._mailbox;
			}
			set
			{
				this._mailbox = value;
				base.OnPropertyChanged(nameof(Mailbox));
			}
		}

		// Token: 0x17000208 RID: 520
		// (get) Token: 0x06000A9C RID: 2716 RVA: 0x0000C791 File Offset: 0x0000A991
		// (set) Token: 0x06000A9D RID: 2717 RVA: 0x0000C799 File Offset: 0x0000A999
		public Server Server
		{
			get
			{
				return this._server;
			}
			set
			{
				this._server = value;
				base.OnPropertyChanged(nameof(Server));
			}
		}

		// Token: 0x17000209 RID: 521
		// (get) Token: 0x06000A9E RID: 2718 RVA: 0x0000C7B2 File Offset: 0x0000A9B2
		// (set) Token: 0x06000A9F RID: 2719 RVA: 0x0000C7BA File Offset: 0x0000A9BA
		public Imap Imap
		{
			get
			{
				return this._imap;
			}
			set
			{
				this._imap = value;
				base.OnPropertyChanged(nameof(Imap));
			}
		}

		// Token: 0x1700020A RID: 522
		// (get) Token: 0x06000AA0 RID: 2720 RVA: 0x0000C7D3 File Offset: 0x0000A9D3
		// (set) Token: 0x06000AA1 RID: 2721 RVA: 0x0000C7DB File Offset: 0x0000A9DB
		public ImapFolder SelectedFolder
		{
			get
			{
				return this._selectedFolder;
			}
			set
			{
				this._selectedFolder = value;
				base.OnPropertyChanged(nameof(SelectedFolder));
			}
		}

		// Token: 0x1700020B RID: 523
		// (get) Token: 0x06000AA2 RID: 2722 RVA: 0x0000C7F4 File Offset: 0x0000A9F4
		// (set) Token: 0x06000AA3 RID: 2723 RVA: 0x0000C7FC File Offset: 0x0000A9FC
		public int Limit
		{
			get
			{
				return this._limit;
			}
			set
			{
				this._limit = value;
				base.OnPropertyChanged(nameof(Limit));
			}
		}

		// Token: 0x1700020C RID: 524
		// (get) Token: 0x06000AA4 RID: 2724 RVA: 0x0000C815 File Offset: 0x0000AA15
		// (set) Token: 0x06000AA5 RID: 2725 RVA: 0x0000C81D File Offset: 0x0000AA1D
		public int ReconnectLimit
		{
			get
			{
				return this._reconnectLimit;
			}
			set
			{
				this._reconnectLimit = value;
				base.OnPropertyChanged(nameof(ReconnectLimit));
			}
		}

		// Token: 0x1700020D RID: 525
		// (get) Token: 0x06000AA6 RID: 2726 RVA: 0x0000C836 File Offset: 0x0000AA36
		// (set) Token: 0x06000AA7 RID: 2727 RVA: 0x0000C83E File Offset: 0x0000AA3E
		public int MaxPageCount
		{
			get
			{
				return this._maxPageCount;
			}
			set
			{
				this._maxPageCount = value;
				base.OnPropertyChanged(nameof(MaxPageCount));
			}
		}

		// Token: 0x1700020E RID: 526
		// (get) Token: 0x06000AA8 RID: 2728 RVA: 0x0000C857 File Offset: 0x0000AA57
		// (set) Token: 0x06000AA9 RID: 2729 RVA: 0x0000C85F File Offset: 0x0000AA5F
		public int PageIndex
		{
			get
			{
				return this._pageIndex;
			}
			set
			{
				this._pageIndex = value;
				base.OnPropertyChanged(nameof(PageIndex));
				base.OnPropertyChanged(nameof(PageIndex));
				base.OnPropertyChanged(nameof(PageIndex));
			}
		}

		// Token: 0x1700020F RID: 527
		// (get) Token: 0x06000AAA RID: 2730 RVA: 0x0000C898 File Offset: 0x0000AA98
		// (set) Token: 0x06000AAB RID: 2731 RVA: 0x0000C8A0 File Offset: 0x0000AAA0
		public SearchQuery SearchQuery
		{
			get
			{
				return this._searchQuery;
			}
			set
			{
				this._searchQuery = value;
				base.OnPropertyChanged(nameof(SearchQuery));
			}
		}

		// Token: 0x17000210 RID: 528
		// (get) Token: 0x06000AAC RID: 2732 RVA: 0x0000C8B9 File Offset: 0x0000AAB9
		// (set) Token: 0x06000AAD RID: 2733 RVA: 0x0000C8C1 File Offset: 0x0000AAC1
		public SearchQuery SavedSearchQuery
		{
			get
			{
				return this._savedSearchQuery;
			}
			set
			{
				this._savedSearchQuery = value;
				base.OnPropertyChanged(nameof(SavedSearchQuery));
			}
		}

		// Token: 0x17000211 RID: 529
		// (get) Token: 0x06000AAE RID: 2734 RVA: 0x0000C8DA File Offset: 0x0000AADA
		// (set) Token: 0x06000AAF RID: 2735 RVA: 0x0000C8E2 File Offset: 0x0000AAE2
		public bool IsImapBusy
		{
			get
			{
				return this._isImapBusy;
			}
			set
			{
				this._isImapBusy = value;
				base.OnPropertyChanged(nameof(IsImapBusy));
			}
		}

		// Token: 0x17000212 RID: 530
		// (get) Token: 0x06000AB0 RID: 2736 RVA: 0x0000C8FB File Offset: 0x0000AAFB
		// (set) Token: 0x06000AB1 RID: 2737 RVA: 0x0000C903 File Offset: 0x0000AB03
		public bool IsSmtpBusy
		{
			get
			{
				return this._isSmtpBusy;
			}
			set
			{
				this._isSmtpBusy = value;
				base.OnPropertyChanged(nameof(IsSmtpBusy));
			}
		}

		// Token: 0x17000213 RID: 531
		// (get) Token: 0x06000AB2 RID: 2738 RVA: 0x0000C91C File Offset: 0x0000AB1C
		// (set) Token: 0x06000AB3 RID: 2739 RVA: 0x0000C924 File Offset: 0x0000AB24
		public string ImapOperationStatus
		{
			get
			{
				return this._imapOperationStatus;
			}
			set
			{
				this._imapOperationStatus = value;
				base.OnPropertyChanged(nameof(ImapOperationStatus));
			}
		}

		// Token: 0x17000214 RID: 532
		// (get) Token: 0x06000AB4 RID: 2740 RVA: 0x0000C93D File Offset: 0x0000AB3D
		// (set) Token: 0x06000AB5 RID: 2741 RVA: 0x0000C945 File Offset: 0x0000AB45
		public string SmtpOperationStatus
		{
			get
			{
				return this._smtpOperationStatus;
			}
			set
			{
				this._smtpOperationStatus = value;
				base.OnPropertyChanged(nameof(SmtpOperationStatus));
			}
		}

		// Token: 0x17000215 RID: 533
		// (get) Token: 0x06000AB6 RID: 2742 RVA: 0x0000C95E File Offset: 0x0000AB5E
		// (set) Token: 0x06000AB7 RID: 2743 RVA: 0x0000C966 File Offset: 0x0000AB66
		public LastOperation LastOperation
		{
			get
			{
				return this._lastOperation;
			}
			set
			{
				this._lastOperation = value;
				base.OnPropertyChanged(nameof(LastOperation));
				base.OnPropertyChanged(nameof(LastOperation));
			}
		}

		// Token: 0x17000216 RID: 534
		// (get) Token: 0x06000AB8 RID: 2744 RVA: 0x0000C98F File Offset: 0x0000AB8F
		// (set) Token: 0x06000AB9 RID: 2745 RVA: 0x0000C997 File Offset: 0x0000AB97
		public bool IsExtendedSelectionMode
		{
			get
			{
				return this._isExtendedSelectionMode;
			}
			set
			{
				this._isExtendedSelectionMode = value;
				base.OnPropertyChanged(nameof(IsExtendedSelectionMode));
			}
		}

		// Token: 0x17000217 RID: 535
		// (get) Token: 0x06000ABA RID: 2746 RVA: 0x0000C9B0 File Offset: 0x0000ABB0
		// (set) Token: 0x06000ABB RID: 2747 RVA: 0x0000C9B8 File Offset: 0x0000ABB8
		public MailMessage Message
		{
			get
			{
				return this._message;
			}
			set
			{
				this._message = value;
				base.OnPropertyChanged(nameof(Message));
			}
		}

		// Token: 0x17000218 RID: 536
		// (get) Token: 0x06000ABC RID: 2748 RVA: 0x0000C9D1 File Offset: 0x0000ABD1
		// (set) Token: 0x06000ABD RID: 2749 RVA: 0x0000C9D9 File Offset: 0x0000ABD9
		public ObservableCollection<Hackus_Mail_Checker_Reforged.Components.Viewer.Models.Attachment> Attachments
		{
			get
			{
				return this._attachments;
			}
			set
			{
				this._attachments = value;
				base.OnPropertyChanged(nameof(Message));
			}
		}

		// Token: 0x17000219 RID: 537
		// (get) Token: 0x06000ABE RID: 2750 RVA: 0x0000C9F2 File Offset: 0x0000ABF2
		// (set) Token: 0x06000ABF RID: 2751 RVA: 0x0000C9FA File Offset: 0x0000ABFA
		public bool ShowMessageBody
		{
			get
			{
				return this._showMessageBody;
			}
			set
			{
				this._showMessageBody = value;
				base.OnPropertyChanged(nameof(ShowMessageBody));
			}
		}

		// Token: 0x1700021A RID: 538
		// (get) Token: 0x06000AC0 RID: 2752 RVA: 0x0000CA13 File Offset: 0x0000AC13
		// (set) Token: 0x06000AC1 RID: 2753 RVA: 0x0000CA1B File Offset: 0x0000AC1B
		public bool ShowAttachments
		{
			get
			{
				return this._showAttachments;
			}
			set
			{
				this._showAttachments = value;
				base.OnPropertyChanged(nameof(ShowAttachments));
			}
		}

		// Token: 0x1700021B RID: 539
		// (get) Token: 0x06000AC2 RID: 2754 RVA: 0x0000CA34 File Offset: 0x0000AC34
		public bool CanMoveForward
		{
			get
			{
				return this.MaxPageCount > this.PageIndex;
			}
		}

		// Token: 0x1700021C RID: 540
		// (get) Token: 0x06000AC3 RID: 2755 RVA: 0x0000CA44 File Offset: 0x0000AC44
		public bool CanMoveBack
		{
			get
			{
				return this.PageIndex > 1;
			}
		}

		// Token: 0x1700021D RID: 541
		// (get) Token: 0x06000AC4 RID: 2756 RVA: 0x0000CA4F File Offset: 0x0000AC4F
		public bool IsLastOperationSearch
		{
			get
			{
				return this.LastOperation == LastOperation.Search;
			}
		}

		// Token: 0x06000AC5 RID: 2757 RVA: 0x0003CD64 File Offset: 0x0003AF64
		private void OnEnvelopeDownloaded(object sender, ImapEnvelopeDownloadedEventArgs e)
		{
			if (!string.IsNullOrEmpty(e.DownloadedEnvelope.From))
			{
				Application.Current.Dispatcher.Invoke(delegate()
				{
					if (this.Messages != null)
						this.Messages.Add(e.DownloadedEnvelope);
				});
			}
		}

		// Token: 0x06000AC6 RID: 2758 RVA: 0x0003CDC0 File Offset: 0x0003AFC0
		private async Task DownloadFoldersAsync()
		{
			await Task.Run(() =>
			{
				try
				{
					var folderList = this.Imap.DownloadFolders();
					Application.Current.Dispatcher.Invoke(() =>
					{
						this.Folders.Clear();
						if (folderList != null)
						{
							foreach (FolderStatus folder in folderList)
							{
								string folderName = folder.FolderName;
								string[] parts = folderName.Split(new char[] { '/', '.' });
								this.ProcessFolder(parts, this.Folders, folderName);
							}
						}
					});
				}
				catch { }
			});
		}

		// Token: 0x06000AC7 RID: 2759 RVA: 0x0003CE04 File Offset: 0x0003B004
		private void ProcessFolder(string[] parts, ObservableCollection<ImapFolder> folders, string fullName)
		{
			foreach (ImapFolder imapFolder in folders)
			{
				string b = parts[0];
				string[] array = parts.Skip(1).ToArray<string>();
				if (imapFolder.Name == b && array.Length == 0)
				{
					return;
				}
				if (imapFolder.Name == b)
				{
					this.ProcessFolder(array, imapFolder.InnerFolders, fullName);
					return;
				}
			}
			string name = parts[0];
			string[] array2 = parts.Skip(1).ToArray<string>();
			ImapFolder imapFolder2 = new ImapFolder(name, fullName);
			folders.Add(imapFolder2);
			if (array2.Length != 0)
			{
				this.ProcessFolder(array2, imapFolder2.InnerFolders, fullName);
			}
		}

		// Token: 0x06000AC8 RID: 2760 RVA: 0x0003CEC0 File Offset: 0x0003B0C0
		private async Task OpenFolder(ImapFolder folder)
		{
			this.SelectedFolder = folder;
			this.SearchedUids = null;
			this.LastOperation = LastOperation.OpenFolder;
			try
			{
				await Task.Run(() =>
				{
					this.Imap.SelectFolder(folder.FullName);
					folder.MessagesCount = this.Imap.MessageCount;
				});
			}
			catch { }
			await this.DownloadMailHeaders(1);
		}

		// Token: 0x1700021E RID: 542
		// (get) Token: 0x06000AC9 RID: 2761 RVA: 0x0003CF0C File Offset: 0x0003B10C
		public RelayCommand SelectFolderCommand
		{
			get
			{
				if (this._selectFolderCommand == null)
				{
					this._selectFolderCommand = new RelayCommand(async (obj) =>
					{
						ImapFolder folder = obj as ImapFolder;
						if (folder != null)
							await this.OpenFolder(folder);
					}, null);
				}
				return this._selectFolderCommand;
			}
		}

		// Token: 0x1700021F RID: 543
		// (get) Token: 0x06000ACA RID: 2762 RVA: 0x0003CF40 File Offset: 0x0003B140
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
						if (this.LastOperation == LastOperation.Search && this.SearchedUids != null)
							await this.DownloadMailHeaders(this.SearchedUids, page);
						else
							await this.DownloadMailHeaders(page);
					}, null);
				}
				return this._updatePageCommand;
			}
		}

		// Token: 0x06000ACB RID: 2763 RVA: 0x0003CF74 File Offset: 0x0003B174
		private async Task<UidCollection> Search(string searchQuery)
		{
			return await Task.Run(() =>
			{
				try
				{
					return this.Imap.Search(true, searchQuery, "UTF-8");
				}
				catch
				{
					return new UidCollection();
				}
			});
		}

		// Token: 0x06000ACC RID: 2764 RVA: 0x0003CFC0 File Offset: 0x0003B1C0
		private async Task DownloadMailHeaders(int offset)
		{
			if (this.Imap == null) return;
			try
			{
				this.IsImapBusy = true;
				this.ImapOperationStatus = "Loading messages...";
				int totalMessages = 0;
				await Task.Run(() => { try { totalMessages = this.Imap.MessageCount; } catch { } });
				this.MaxPageCount = Math.Max(1, (totalMessages + this.Limit - 1) / this.Limit);
				this.PageIndex = Math.Max(1, Math.Min(offset, this.MaxPageCount));
				int endIndex = totalMessages - (this.PageIndex - 1) * this.Limit;
				int startIndex = Math.Max(1, endIndex - this.Limit + 1);
				Application.Current.Dispatcher.Invoke(() => this.Messages.Clear());
				if (totalMessages > 0 && startIndex <= endIndex)
				{
					string range = startIndex + ":" + endIndex;
					await Task.Run(() => { try { this.Imap.DownloadEnvelopes(range, false); } catch { } });
				}
				this.ImapOperationStatus = "Loaded " + this.Messages.Count + " messages";
			}
			catch (Exception ex)
			{
				this.ImapOperationStatus = "Error: " + ex.Message;
			}
			finally
			{
				this.IsImapBusy = false;
			}
		}

		// Token: 0x06000ACD RID: 2765 RVA: 0x0003D00C File Offset: 0x0003B20C
		private async Task DownloadMailHeaders(UidCollection uids)
		{
			await this.DownloadMailHeaders(uids, 1);
		}

		// Token: 0x06000ACE RID: 2766 RVA: 0x0003D058 File Offset: 0x0003B258
		private async Task DownloadMailHeaders(UidCollection uids, int offset)
		{
			if (this.Imap == null || uids == null) return;
			try
			{
				this.IsImapBusy = true;
				this.ImapOperationStatus = "Loading messages...";
				int totalUids = uids.Count;
				this.MaxPageCount = Math.Max(1, (totalUids + this.Limit - 1) / this.Limit);
				this.PageIndex = Math.Max(1, Math.Min(offset, this.MaxPageCount));
				int startIdx = (this.PageIndex - 1) * this.Limit;
				int endIdx = Math.Min(startIdx + this.Limit, totalUids);
				Application.Current.Dispatcher.Invoke(() => this.Messages.Clear());
				if (startIdx < endIdx)
				{
					UidCollection pageUidCollection = new UidCollection();
					for (int i = startIdx; i < endIdx; i++)
						pageUidCollection.Add(uids[i]);
					await Task.Run(() => { try { this.Imap.UidMode = true; this.Imap.DownloadEnvelopes(string.Join(",", pageUidCollection), false); } catch { } finally { this.Imap.UidMode = false; } });
				}
				this.ImapOperationStatus = "Loaded " + this.Messages.Count + " messages";
			}
			catch (Exception ex)
			{
				this.ImapOperationStatus = "Error: " + ex.Message;
			}
			finally
			{
				this.IsImapBusy = false;
			}
		}

		// Token: 0x17000220 RID: 544
		// (get) Token: 0x06000ACF RID: 2767 RVA: 0x0003D0AC File Offset: 0x0003B2AC
		public RelayCommand OpenMessageCommand
		{
			get
			{
				if (this._openMessageCommand == null)
				{
					this._openMessageCommand = new RelayCommand(async (obj) =>
					{
						Envelope envelope = obj as Envelope;
						if (envelope == null) return;
						try
						{
							this.IsImapBusy = true;
							this.ImapOperationStatus = "Loading message...";
							MailMessage msg = await Task.Run(() =>
							{
								try { this.Imap.UidMode = true; return this.Imap.DownloadEntireMessage(envelope.Uid); }
								catch { return null; }
								finally { this.Imap.UidMode = false; }
							});
							if (msg != null)
							{
								this.Message = msg;
								this.ShowMessageBody = true;
							}
						}
						catch (Exception ex)
						{
							this.ImapOperationStatus = "Error: " + ex.Message;
						}
						finally
						{
							this.IsImapBusy = false;
						}
					}, null);
				}
				return this._openMessageCommand;
			}
		}

		// Token: 0x17000221 RID: 545
		// (get) Token: 0x06000AD0 RID: 2768 RVA: 0x0003D0E0 File Offset: 0x0003B2E0
		public RelayCommand DeleteMessagesCommand
		{
			get
			{
				if (this._deleteMessagesCommand == null)
				{
					this._deleteMessagesCommand = new RelayCommand(async (obj) =>
					{
						try
						{
							this.IsImapBusy = true;
							this.ImapOperationStatus = "Deleting messages...";
							Envelope[] toDelete = this.SelectedMessages.ToArray();
							await Task.Run(() =>
							{
								try
								{
									this.Imap.UidMode = true;
									foreach (var env in toDelete)
										this.Imap.SetMessageFlags(env.Uid, MessageFlags.Deleted, true);
									this.Imap.Expunge();
								}
								catch { }
								finally { this.Imap.UidMode = false; }
							});
							Application.Current.Dispatcher.Invoke(() =>
							{
								foreach (var env in toDelete)
									this.Messages.Remove(env);
								this.SelectedMessages.Clear();
							});
							this.ImapOperationStatus = "Messages deleted";
						}
						catch (Exception ex)
						{
							this.ImapOperationStatus = "Error: " + ex.Message;
						}
						finally
						{
							this.IsImapBusy = false;
						}
					}, null);
				}
				return this._deleteMessagesCommand;
			}
		}

		// Token: 0x17000222 RID: 546
		// (get) Token: 0x06000AD1 RID: 2769 RVA: 0x0003D114 File Offset: 0x0003B314
		public RelayCommand SearchCommand
		{
			get
			{
				if (this._searchCommand == null)
				{
					this._searchCommand = new RelayCommand(async (obj) =>
					{
						if (string.IsNullOrWhiteSpace(this.SearchQuery.Query) && this.SearchQuery.DateFrom == null && this.SearchQuery.DateTo == null)
							return;
						try
						{
							this.IsImapBusy = true;
							this.ImapOperationStatus = "Searching...";
							this.SavedSearchQuery = this.SearchQuery.Clone();
							string criteria = this.BuildSearchQuery(this.SavedSearchQuery);
							UidCollection uids = await this.Search(criteria);
							this.SearchedUids = uids;
							this.LastOperation = LastOperation.Search;
							await this.DownloadMailHeaders(uids, 1);
						}
						catch (Exception ex)
						{
							this.ImapOperationStatus = "Error: " + ex.Message;
						}
						finally
						{
							this.IsImapBusy = false;
						}
					}, null);
				}
				return this._searchCommand;
			}
		}

		// Token: 0x06000AD2 RID: 2770 RVA: 0x0003D148 File Offset: 0x0003B348
		private string BuildSearchQuery(SearchQuery searchQuery)
		{
			StringBuilder stringBuilder = new StringBuilder();
			SearchType type = searchQuery.Type;
			if (type != SearchType.Sender)
			{
				if (type == SearchType.Body)
				{
					stringBuilder.Append("BODY " + ImapUtils.ToQuotedString(searchQuery.Query));
				}
			}
			else
			{
				stringBuilder.Append("FROM " + ImapUtils.ToQuotedString(searchQuery.Query));
			}
			if (searchQuery.DateFrom != null)
			{
				string str = searchQuery.DateFrom.Value.ToString("dd-MMM-yyyy", CultureInfo.CreateSpecificCulture("en-US"));
				stringBuilder.Append(" SINCE " + str);
			}
			if (searchQuery.DateTo != null)
			{
				string str2 = searchQuery.DateTo.Value.ToString("dd-MMM-yyyy", CultureInfo.CreateSpecificCulture("en-US"));
				stringBuilder.Append(" BEFORE " + str2);
			}
			return stringBuilder.ToString();
		}

		// Token: 0x17000223 RID: 547
		// (get) Token: 0x06000AD3 RID: 2771 RVA: 0x0003D268 File Offset: 0x0003B468
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

		// Token: 0x17000224 RID: 548
		// (get) Token: 0x06000AD4 RID: 2772 RVA: 0x0003D29C File Offset: 0x0003B49C
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

		// Token: 0x17000225 RID: 549
		// (get) Token: 0x06000AD5 RID: 2773 RVA: 0x0003D2E4 File Offset: 0x0003B4E4
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

		// Token: 0x06000AD6 RID: 2774 RVA: 0x0003D32C File Offset: 0x0003B52C
		private async Task<bool> EstablishConnection(bool openFolder = false)
		{
			int reconnectCount = 0;
			while (reconnectCount < this.ReconnectLimit)
			{
				try
				{
					if (this.Imap != null)
					{
						this.Imap.EnvelopeDownloaded -= this.OnEnvelopeDownloaded;
						try { await Task.Run(() => this.Imap.Disconnect()); } catch { }
					}
					Imap newImap = new Imap();
					newImap.SslMode = (this.Server.Socket == SocketType.SSL)
						? SslStartupMode.OnConnect
						: SslStartupMode.Manual;
					newImap.Timeout = ViewerSettings.Instance.Timeout * 1000;
					bool connected = await Task.Run(() =>
					{
						try
						{
							newImap.Connect(this.Server.Hostname, this.Server.Port);
							newImap.Login(this.Mailbox.Address, this.Mailbox.Password);
							return true;
						}
						catch { return false; }
					});
					if (connected)
					{
						this.Imap = newImap;
						this.Imap.EnvelopeDownloaded += this.OnEnvelopeDownloaded;
						if (openFolder && this.SelectedFolder != null)
						{
							try
							{
								await Task.Run(() => this.Imap.SelectFolder(this.SelectedFolder.FullName));
							}
							catch { }
							await this.DownloadMailHeaders(this.PageIndex > 0 ? this.PageIndex : 1);
						}
						return true;
					}
				}
				catch { }
				reconnectCount++;
			}
			return false;
		}

		// Token: 0x17000226 RID: 550
		// (get) Token: 0x06000AD7 RID: 2775 RVA: 0x0003D378 File Offset: 0x0003B578
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
						try
						{
							this.IsImapBusy = true;
							await this.DownloadFoldersAsync();
							ImapFolder inbox = this.Folders.FirstOrDefault(f => f.Name.Equals("INBOX", StringComparison.OrdinalIgnoreCase));
							if (inbox == null) inbox = this.Folders.FirstOrDefault();
							if (inbox != null)
								await this.OpenFolder(inbox);
						}
						finally
						{
							this.IsImapBusy = false;
						}
						this.InitializeKeepAlive();
					}, null);
				}
				return this._initializeCommand;
			}
		}

		// Token: 0x17000227 RID: 551
		// (get) Token: 0x06000AD8 RID: 2776 RVA: 0x0003D3AC File Offset: 0x0003B5AC
		public RelayCommand ChangeSelectionModeCommand
		{
			get
			{
				RelayCommand result;
				if ((result = this._changeSelectionModeCommand) == null)
				{
					result = (this._changeSelectionModeCommand = new RelayCommand(delegate(object obj)
					{
						this.IsExtendedSelectionMode = !this.IsExtendedSelectionMode;
					}, null));
				}
				return result;
			}
		}

		// Token: 0x17000228 RID: 552
		// (get) Token: 0x06000AD9 RID: 2777 RVA: 0x0003D3E0 File Offset: 0x0003B5E0
		public RelayCommand RefreshCommand
		{
			get
			{
				RelayCommand result;
				if ((result = this._refreshCommand) == null)
				{
					result = (this._refreshCommand = new RelayCommand(delegate(object obj)
					{
						this.UpdatePageCommand.Execute(new FunctionEventArgs<int>(this.PageIndex));
					}, null));
				}
				return result;
			}
		}

		// Token: 0x17000229 RID: 553
		// (get) Token: 0x06000ADA RID: 2778 RVA: 0x0003D414 File Offset: 0x0003B614
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

		// Token: 0x1700022A RID: 554
		// (get) Token: 0x06000ADB RID: 2779 RVA: 0x0003D448 File Offset: 0x0003B648
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

		// Token: 0x1700022B RID: 555
		// (get) Token: 0x06000ADC RID: 2780 RVA: 0x0003D47C File Offset: 0x0003B67C
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

		// Token: 0x1700022C RID: 556
		// (get) Token: 0x06000ADD RID: 2781 RVA: 0x0003D4B0 File Offset: 0x0003B6B0
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
							if (this.Imap != null && this.Imap.IsConnected)
								await Task.Run(() => { try { this.Imap.Disconnect(); } catch { } });
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

		// Token: 0x1700022D RID: 557
		// (get) Token: 0x06000ADE RID: 2782 RVA: 0x0003D4E4 File Offset: 0x0003B6E4
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

		// Token: 0x1700022E RID: 558
		// (get) Token: 0x06000ADF RID: 2783 RVA: 0x0003D518 File Offset: 0x0003B718
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

		// Token: 0x1700022F RID: 559
		// (get) Token: 0x06000AE0 RID: 2784 RVA: 0x0003D54C File Offset: 0x0003B74C
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

		// Token: 0x06000AE1 RID: 2785 RVA: 0x0000CA5A File Offset: 0x0000AC5A
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
				if (this.Imap != null && this.Imap.IsConnected)
					await Task.Run(() => this.Imap.Noop());
			}
			catch
			{
				await ReconnectAsync();
			}
		}

		private async Task ReconnectAsync()
		{
			bool success = await this.EstablishConnection(openFolder: true);
			if (!success)
			{
				_keepAliveTimer?.Stop();
			}
		}

		// Token: 0x06000AE2 RID: 2786 RVA: 0x00006BF6 File Offset: 0x00004DF6
		public string Resource(string key)
		{
			return ResourceHelper.GetResource<string>(key);
		}

		// Token: 0x0400056A RID: 1386
		private bool _isInitialized;

		// Token: 0x0400056B RID: 1387
		private bool _isDisposed;

		// Token: 0x0400056C RID: 1388
		private TranslationLanguage _translationFromLanguage;

		// Token: 0x0400056D RID: 1389
		private TranslationLanguage _translationToLanguage;

		// Token: 0x0400056E RID: 1390
		private ObservableCollection<ImapFolder> _folders;

		// Token: 0x0400056F RID: 1391
		private ObservableCollection<Envelope> _messages;

		// Token: 0x04000570 RID: 1392
		private ObservableCollection<Envelope> _selectedMessages;

		// Token: 0x04000571 RID: 1393
		private UidCollection _searchedUids;

		// Token: 0x04000572 RID: 1394
		private Mailbox _mailbox;

		// Token: 0x04000573 RID: 1395
		private Server _server;

		// Token: 0x04000574 RID: 1396
		private Imap _imap;

		// Token: 0x04000575 RID: 1397
		private ImapFolder _selectedFolder;

		// Token: 0x04000576 RID: 1398
		private int _limit = 50;

		// Token: 0x04000577 RID: 1399
		private int _reconnectLimit = 1;

		// Token: 0x04000578 RID: 1400
		private int _maxPageCount = 1;

		// Token: 0x04000579 RID: 1401
		private int _pageIndex;

		// Token: 0x0400057A RID: 1402
		private SearchQuery _searchQuery;

		// Token: 0x0400057B RID: 1403
		private SearchQuery _savedSearchQuery;

		// Token: 0x0400057C RID: 1404
		private bool _isImapBusy;

		// Token: 0x0400057D RID: 1405
		private bool _isSmtpBusy;

		// Token: 0x0400057E RID: 1406
		private string _imapOperationStatus;

		// Token: 0x0400057F RID: 1407
		private string _smtpOperationStatus;

		// Token: 0x04000580 RID: 1408
		private LastOperation _lastOperation;

		// Token: 0x04000581 RID: 1409
		private bool _isExtendedSelectionMode;

		// Token: 0x04000582 RID: 1410
		private MailMessage _message;

		// Token: 0x04000583 RID: 1411
		private ObservableCollection<Hackus_Mail_Checker_Reforged.Components.Viewer.Models.Attachment> _attachments;

		// Token: 0x04000584 RID: 1412
		private bool _showMessageBody;

		// Token: 0x04000585 RID: 1413
		private bool _showAttachments;

		// Token: 0x04000586 RID: 1414
		private RelayCommand _selectFolderCommand;

		// Token: 0x04000587 RID: 1415
		private RelayCommand _updatePageCommand;

		// Token: 0x04000588 RID: 1416
		private RelayCommand _openMessageCommand;

		// Token: 0x04000589 RID: 1417
		private RelayCommand _deleteMessagesCommand;

		// Token: 0x0400058A RID: 1418
		private RelayCommand _searchCommand;

		// Token: 0x0400058B RID: 1419
		private RelayCommand _switchAttachmentsModeCommand;

		// Token: 0x0400058C RID: 1420
		private RelayCommand _downloadAttachmentCommand;

		// Token: 0x0400058D RID: 1421
		private RelayCommand _downloadAttachmentByPathCommand;

		// Token: 0x0400058E RID: 1422
		private RelayCommand _initializeCommand;

		// Token: 0x0400058F RID: 1423
		private RelayCommand _changeSelectionModeCommand;

		// Token: 0x04000590 RID: 1424
		private RelayCommand _refreshCommand;

		// Token: 0x04000591 RID: 1425
		private RelayCommand _openMessagesListCommand;

		// Token: 0x04000592 RID: 1426
		private RelayCommand _goForwardCommand;

		// Token: 0x04000593 RID: 1427
		private RelayCommand _goBackCommand;

		// Token: 0x04000594 RID: 1428
		private RelayCommand _disconnectCommand;

		// Token: 0x04000595 RID: 1429
		private RelayCommand _changeTranslationFromLanguageCommand;

		// Token: 0x04000596 RID: 1430
		private RelayCommand _changeTranslationToLanguageCommand;

		// Token: 0x04000597 RID: 1431
		private RelayCommand _translateCommand;
	}
}
