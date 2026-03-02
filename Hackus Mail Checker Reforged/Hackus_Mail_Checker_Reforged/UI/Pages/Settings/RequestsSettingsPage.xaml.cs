using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using Hackus_Mail_Checker_Reforged.UI.Pages.Popups;
using Hackus_Mail_Checker_Reforged.UI.ViewModels;
using Hackus_Mail_Checker_Reforged.UI.Views;
using HandyControl.Controls;
using HandyControl.Tools;

namespace Hackus_Mail_Checker_Reforged.UI.Pages.Settings
{
	// Token: 0x02000044 RID: 68
	public partial class RequestsSettingsPage : Page
	{
		// Token: 0x0600022B RID: 555 RVA: 0x00007556 File Offset: 0x00005756
		public RequestsSettingsPage()
		{
			this.InitializeComponent();
			base.DataContext = MainViewModel.Instance;
		}

		// Token: 0x0600022C RID: 556 RVA: 0x00016330 File Offset: 0x00014530
		private void SearchRequestComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (this.SearchRequestFormControl == null)
			{
				return;
			}
			string name = ((ComboBoxItem)this.SearchRequestComboBox.SelectedItem).Name;
			uint num = _PrivateImplementationDetails_.ComputeStringHash(name);
			if (num > 1788324655U)
			{
				if (num <= 1912235038U)
				{
					if (num != 1841104093U)
					{
						if (num != 1912235038U)
						{
							return;
						}
						if (!(name == "SearchByContentItem"))
						{
							return;
						}
						this.SearchRequestFormControl.Content = base.Resources["AddSearchRequestByContentControl"];
						return;
					}
					else
					{
						if (name == "SearchBySenderAndSubjectItem")
						{
							this.SearchRequestFormControl.Content = base.Resources["AddSearchRequestBySenderAndSubjectControl"];
							return;
						}
						return;
					}
				}
				else if (num == 3884550458U)
				{
					if (name == "SearchBySenderItem")
					{
						this.SearchRequestFormControl.Content = base.Resources["AddSearchRequestBySenderControl"];
						return;
					}
					return;
				}
				else
				{
					if (num != 4144389860U)
					{
						return;
					}
					if (name == "SearchBySenderAndContentItem")
					{
						this.SearchRequestFormControl.Content = base.Resources["AddSearchRequestBySenderAndContentControl"];
						return;
					}
					return;
				}
			}
			else if (num == 640711487U)
			{
				if (name == "SearchBySenderAndSubjectAndContentItem")
				{
					this.SearchRequestFormControl.Content = base.Resources["AddSearchRequestBySenderAndSubjectAndContentControl"];
					return;
				}
				return;
			}
			else if (num != 775638365U)
			{
				if (num != 1788324655U)
				{
					return;
				}
				if (!(name == "SearchBySubjectItem"))
				{
					return;
				}
				this.SearchRequestFormControl.Content = base.Resources["AddSearchRequestBySubjectControl"];
				return;
			}
			else
			{
				if (name == "SearchBySubjectAndContentItem")
				{
					this.SearchRequestFormControl.Content = base.Resources["AddSearchRequestBySubjectAndContentControl"];
					return;
				}
				return;
			}
		}

		// Token: 0x0600022D RID: 557 RVA: 0x0001651C File Offset: 0x0001471C
		private void ManageRequestGroups(object sender, RoutedEventArgs e)
		{
			SearchGroups searchGroups = SingleOpenHelper.CreateControl<SearchGroups>();
			Point position = Mouse.GetPosition(Application.Current.MainWindow);
			PopupWindow window = new PopupWindow
			{
				Left = position.X - 200.0,
				Top = position.Y + 50.0,
				PopupElement = searchGroups,
				Owner = Application.Current.Windows.OfType<MainView>().First<MainView>()
			};
			searchGroups.Canceled += delegate(object sender, EventArgs e)
			{
				window.Close();
			};
			window.Show(window, false);
		}
	}
}
