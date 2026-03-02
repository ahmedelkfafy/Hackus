using System;
using System.Globalization;
using System.Windows.Data;
using Hackus_Mail_Checker_Reforged.Services.Managers;

namespace Hackus_Mail_Checker_Reforged.Resources.Converters
{
	// Token: 0x0200008F RID: 143
	public class StatisticsToProgressTextConverter : IMultiValueConverter
	{
		// Token: 0x060004D9 RID: 1241 RVA: 0x0001B0E8 File Offset: 0x000192E8
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			string text = StatisticsManager.Instance.CheckedStrings.ToString();
			string text2 = StatisticsManager.Instance.LoadedStrings.ToString();
			if (!(text2 == "0"))
			{
				if (text.Length > 3 && text.Length < 7)
				{
					text = text.Substring(0, text.Length - 3) + "K";
				}
				else if (text.Length > 6)
				{
					text = text.Substring(0, text.Length - 6) + "M";
				}
				if (text2.Length > 3 && text2.Length < 7)
				{
					text2 = text2.Substring(0, text2.Length - 3) + "K";
				}
				else if (text2.Length > 6)
				{
					text2 = text2.Substring(0, text2.Length - 6) + "M";
				}
				return text + " | " + text2;
			}
			return "∞";
		}

		// Token: 0x060004DA RID: 1242 RVA: 0x00009889 File Offset: 0x00007A89
		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			return new object[0];
		}
	}
}
