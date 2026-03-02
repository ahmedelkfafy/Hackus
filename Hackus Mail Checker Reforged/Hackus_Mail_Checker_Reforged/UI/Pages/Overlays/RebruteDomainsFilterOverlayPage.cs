using System.Windows.Controls;
using Hackus_Mail_Checker_Reforged.UI.ViewModels;

namespace Hackus_Mail_Checker_Reforged.UI.Pages.Overlays
{
	// Token: 0x02000050 RID: 80
	public partial class RebruteDomainsFilterOverlayPage : Page
	{
		// Token: 0x06000281 RID: 641 RVA: 0x000078DD File Offset: 0x00005ADD
		public RebruteDomainsFilterOverlayPage()
		{
			this.InitializeComponent();
			base.DataContext = MainViewModel.Instance;
		}
	}
}
