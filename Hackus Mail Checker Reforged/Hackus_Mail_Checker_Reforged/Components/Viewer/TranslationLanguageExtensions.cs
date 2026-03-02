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
				return _Module_.smethod_4<string>(869848139);
			case TranslationLanguage.English:
				return _Module_.smethod_2<string>(-1746043920);
			case TranslationLanguage.Russian:
				return _Module_.smethod_6<string>(-423417630);
			case TranslationLanguage.Chinese:
				return _Module_.smethod_5<string>(517562853);
			case TranslationLanguage.German:
				return _Module_.smethod_2<string>(625955354);
			case TranslationLanguage.French:
				return _Module_.smethod_6<string>(1872215015);
			case TranslationLanguage.Poland:
				return _Module_.smethod_5<string>(-432193977);
			case TranslationLanguage.Spanish:
				return _Module_.smethod_6<string>(-1680570541);
			default:
				return _Module_.smethod_3<string>(-208523070);
			}
		}
	}
}
