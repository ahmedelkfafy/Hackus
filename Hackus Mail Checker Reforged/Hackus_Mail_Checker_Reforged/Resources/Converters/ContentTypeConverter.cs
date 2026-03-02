using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Hackus_Mail_Checker_Reforged.Resources.Converters
{
	// Token: 0x0200008A RID: 138
	public class ContentTypeConverter : IValueConverter
	{
		// Token: 0x060004CA RID: 1226 RVA: 0x0001AE74 File Offset: 0x00019074
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			string text = value as string;
			if (text == null)
			{
				return DependencyProperty.UnsetValue;
			}
			if (text.ContainsIgnoreCase("image"))
			{
				return "Image";
			}
			if (text.ContainsIgnoreCase("audio"))
			{
				return "Audio";
			}
			if (text.ContainsIgnoreCase("zip"))
			{
				return "Zip";
			}
			if (text.ContainsIgnoreCase("video"))
			{
				return "Video";
			}
			if (text.ContainsIgnoreCase("pdf"))
			{
				return "Pdf";
			}
			return "File";
		}

		// Token: 0x060004CB RID: 1227 RVA: 0x0001AF2C File Offset: 0x0001912C
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			string text = parameter as string;
			if (text == null)
			{
				return DependencyProperty.UnsetValue;
			}
			return Enum.Parse(targetType, text);
		}
	}
}
