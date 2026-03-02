using System;

namespace Hackus_Mail_Checker_Reforged.Components.Viewer
{
	// Token: 0x0200015F RID: 351
	public static class TranslationLanguageExtensions
	{
		// Token: 0x06000A2C RID: 2604 RVA: 0x0003B470 File Offset: 0x00039670
		public static string ToShortString(this TranslationLanguage language)
		{
			switch (language)
			{
			case TranslationLanguage.Auto:
				return "auto";
			case TranslationLanguage.English:
				return "en";
			case TranslationLanguage.Russian:
				return "ru";
			case TranslationLanguage.Chinese:
				return "zh";
			case TranslationLanguage.German:
				return "de";
			case TranslationLanguage.French:
				return "fr";
			case TranslationLanguage.Poland:
				return "pl";
			case TranslationLanguage.Spanish:
				return "es";
			default:
				return "auto";
			}
		}
	}
}
