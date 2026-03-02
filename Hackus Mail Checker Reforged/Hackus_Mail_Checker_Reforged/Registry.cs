using System;
using System.Globalization;
using System.Linq;
using HandyControl.Tools;

namespace Hackus_Mail_Checker_Reforged
{
	// Token: 0x02000020 RID: 32
	internal static class Registry
	{
		// Token: 0x060000AC RID: 172 RVA: 0x00011AB0 File Offset: 0x0000FCB0
		public static ValueTuple<string, string> GetSavedCredentials()
		{
			string value = RegistryHelper.GetValue<string>("Username", "Hackus Apps\\Mail Checker", null);
			string value2 = RegistryHelper.GetValue<string>("Password", "Hackus Apps\\Mail Checker", null);
			return new ValueTuple<string, string>(value, value2);
		}

		// Token: 0x060000AD RID: 173 RVA: 0x000066C1 File Offset: 0x000048C1
		public static void SaveCredentials(string username, string password)
		{
			RegistryHelper.AddOrUpdateKey<string>("Username", "Hackus Apps\\Mail Checker", username, null);
			RegistryHelper.AddOrUpdateKey<string>("Password", "Hackus Apps\\Mail Checker", password, null);
		}

		// Token: 0x060000AE RID: 174 RVA: 0x00011AF8 File Offset: 0x0000FCF8
		public static void SetLanguage()
		{
			if (RegistryHelper.GetValue<string>("Lang", "Hackus Apps\\Mail Checker", null) == "ru-RU")
			{
				App.Language = App.Languages.FirstOrDefault((CultureInfo language) => Registry._c_.smethod_1(Registry._c_.smethod_0(language), "ru-RU"));
				return;
			}
			App.Language = App.Languages.FirstOrDefault((CultureInfo language) => Registry._c_.smethod_1(Registry._c_.smethod_0(language), "en-US"));
		}

		// Token: 0x060000AF RID: 175 RVA: 0x00011B90 File Offset: 0x0000FD90
		public static void SaveLanguage()
		{
			if (App.Language.Name == "ru-RU")
			{
				RegistryHelper.AddOrUpdateKey<string>("Lang", "Hackus Apps\\Mail Checker", "ru-RU", null);
				return;
			}
			RegistryHelper.AddOrUpdateKey<string>("Lang", "Hackus Apps\\Mail Checker", "en-US", null);
		}
	}
}
