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
			string value = RegistryHelper.GetValue<string>(_Module_.smethod_4<string>(-188245252), _Module_.smethod_6<string>(1875065851), null);
			string value2 = RegistryHelper.GetValue<string>(_Module_.smethod_6<string>(2024540070), _Module_.smethod_2<string>(-546547708), null);
			return new ValueTuple<string, string>(value, value2);
		}

		// Token: 0x060000AD RID: 173 RVA: 0x000066C1 File Offset: 0x000048C1
		public static void SaveCredentials(string username, string password)
		{
			RegistryHelper.AddOrUpdateKey<string>(_Module_.smethod_2<string>(-1795053037), _Module_.smethod_5<string>(787785413), username, null);
			RegistryHelper.AddOrUpdateKey<string>(_Module_.smethod_5<string>(-2024528016), _Module_.smethod_5<string>(787785413), password, null);
		}

		// Token: 0x060000AE RID: 174 RVA: 0x00011AF8 File Offset: 0x0000FCF8
		public static void SetLanguage()
		{
			if (RegistryHelper.GetValue<string>(_Module_.smethod_6<string>(1431832549), _Module_.smethod_4<string>(1477444617), null) == _Module_.smethod_4<string>(1128797621))
			{
				App.Language = App.Languages.FirstOrDefault((CultureInfo language) => Registry._c_.smethod_1(Registry._c_.smethod_0(language), _Module_.smethod_3<string>(11138840)));
				return;
			}
			App.Language = App.Languages.FirstOrDefault((CultureInfo language) => Registry._c_.smethod_1(Registry._c_.smethod_0(language), _Module_.smethod_2<string>(1401011641)));
		}

		// Token: 0x060000AF RID: 175 RVA: 0x00011B90 File Offset: 0x0000FD90
		public static void SaveLanguage()
		{
			if (App.Language.Name == _Module_.smethod_2<string>(152506312))
			{
				RegistryHelper.AddOrUpdateKey<string>(_Module_.smethod_4<string>(-1413318057), _Module_.smethod_6<string>(1875065851), _Module_.smethod_2<string>(152506312), null);
				return;
			}
			RegistryHelper.AddOrUpdateKey<string>(_Module_.smethod_3<string>(292081982), _Module_.smethod_6<string>(1875065851), _Module_.smethod_5<string>(1510633816), null);
		}
	}
}
