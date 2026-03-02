using System;
using System.Management;
using System.Threading.Tasks;

namespace Hackus_Mail_Checker_Reforged.Components.Startup
{
	// Token: 0x020001B5 RID: 437
	internal static class HWID
	{
		// Token: 0x06000CAD RID: 3245 RVA: 0x00043778 File Offset: 0x00041978
		public static string Value()
		{
			/*
An exception occurred when decompiling this method (06000CAD)

ICSharpCode.Decompiler.DecompilerException: Error decompiling System.String Hackus_Mail_Checker_Reforged.Components.Startup.HWID::Value()

 ---> System.ArgumentOutOfRangeException: Non-negative number required. (Parameter 'length')
   at System.Array.Copy(Array sourceArray, Int32 sourceIndex, Array destinationArray, Int32 destinationIndex, Int32 length, Boolean reliable)
   at System.Array.Copy(Array sourceArray, Array destinationArray, Int32 length)
   at ICSharpCode.Decompiler.ILAst.ILAstBuilder.StackSlot.ModifyStack(StackSlot[] stack, Int32 popCount, Int32 pushCount, ByteCode pushDefinition) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\ILAst\ILAstBuilder.cs:line 48
   at ICSharpCode.Decompiler.ILAst.ILAstBuilder.StackAnalysis(MethodDef methodDef) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\ILAst\ILAstBuilder.cs:line 387
   at ICSharpCode.Decompiler.ILAst.ILAstBuilder.Build(MethodDef methodDef, Boolean optimize, DecompilerContext context) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\ILAst\ILAstBuilder.cs:line 269
   at ICSharpCode.Decompiler.Ast.AstMethodBodyBuilder.CreateMethodBody(IEnumerable`1 parameters, MethodDebugInfoBuilder& builder) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\Ast\AstMethodBodyBuilder.cs:line 112
   at ICSharpCode.Decompiler.Ast.AstMethodBodyBuilder.CreateMethodBody(MethodDef methodDef, DecompilerContext context, AutoPropertyProvider autoPropertyProvider, IEnumerable`1 parameters, Boolean valueParameterIsKeyword, StringBuilder sb, MethodDebugInfoBuilder& stmtsBuilder) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\Ast\AstMethodBodyBuilder.cs:line 88
   --- End of inner exception stack trace ---
   at ICSharpCode.Decompiler.Ast.AstMethodBodyBuilder.CreateMethodBody(MethodDef methodDef, DecompilerContext context, AutoPropertyProvider autoPropertyProvider, IEnumerable`1 parameters, Boolean valueParameterIsKeyword, StringBuilder sb, MethodDebugInfoBuilder& stmtsBuilder) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\Ast\AstMethodBodyBuilder.cs:line 92
   at ICSharpCode.Decompiler.Ast.AstBuilder.AddMethodBody(EntityDeclaration methodNode, EntityDeclaration& updatedNode, MethodDef method, IEnumerable`1 parameters, Boolean valueParameterIsKeyword, MethodKind methodKind) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\Ast\AstBuilder.cs:line 1533
*/;
		}

		// Token: 0x06000CAE RID: 3246 RVA: 0x0000D88B File Offset: 0x0000BA8B
		public static void Start(Action callback)
		{
			Task.Run(delegate()
			{
				HWID.Value();
				callback();
			});
		}

		// Token: 0x06000CAF RID: 3247 RVA: 0x000437F4 File Offset: 0x000419F4
		private static string GetHexString(byte[] bt)
		{
			string text = string.Empty;
			for (int i = 0; i < bt.Length; i++)
			{
				byte b = bt[i];
				int num = (int)(b & 15);
				int num2 = b >> 4 & 15;
				if (num2 > 9)
				{
					text += ((char)(num2 - 10 + 65)).ToString();
				}
				else
				{
					text += num2.ToString();
				}
				if (num > 9)
				{
					text += ((char)(num - 10 + 65)).ToString();
				}
				else
				{
					text += num.ToString();
				}
				if (i + 1 != bt.Length && (i + 1) % 2 == 0)
				{
					text += "-";
				}
			}
			return text;
		}

		// Token: 0x06000CB0 RID: 3248 RVA: 0x000438A8 File Offset: 0x00041AA8
		private static string identifier(string wmiClass, string wmiProperty, string wmiMustBeTrue)
		{
			string text = "";
			foreach (ManagementBaseObject managementBaseObject in new ManagementClass(wmiClass).GetInstances())
			{
				ManagementObject managementObject = (ManagementObject)managementBaseObject;
				if (managementObject[wmiMustBeTrue].ToString() == "True" && text == "")
				{
					try
					{
						text = managementObject[wmiProperty].ToString();
						break;
					}
					catch
					{
					}
				}
			}
			return text;
		}

		// Token: 0x06000CB1 RID: 3249 RVA: 0x00043948 File Offset: 0x00041B48
		private static string identifier(string wmiClass, string wmiProperty)
		{
			string text = "";
			foreach (ManagementBaseObject managementBaseObject in new ManagementClass(wmiClass).GetInstances())
			{
				ManagementObject managementObject = (ManagementObject)managementBaseObject;
				if (text == "")
				{
					try
					{
						text = managementObject[wmiProperty].ToString();
						break;
					}
					catch
					{
					}
				}
			}
			return text;
		}

		// Token: 0x06000CB2 RID: 3250 RVA: 0x000439CC File Offset: 0x00041BCC
		private static string cpuId()
		{
			string text = HWID.identifier("Win32_Processor", "UniqueId");
			if (text == "")
			{
				text = HWID.identifier("Win32_Processor", "ProcessorId");
				if (text == "")
				{
					text = HWID.identifier("Win32_Processor", "Name");
					if (text == "")
					{
						text = HWID.identifier("Win32_Processor", "Manufacturer");
					}
					text += HWID.identifier("Win32_Processor", "MaxClockSpeed");
				}
			}
			return text;
		}

		// Token: 0x06000CB3 RID: 3251 RVA: 0x00043A8C File Offset: 0x00041C8C
		private static string biosId()
		{
			return HWID.identifier("Win32_BIOS", "Manufacturer") + HWID.identifier("Win32_BIOS", "IdentificationCode") + HWID.identifier("Win32_BIOS", "SerialNumber") + HWID.identifier("Win32_BIOS", "ReleaseDate");
		}

		// Token: 0x06000CB4 RID: 3252 RVA: 0x00043B04 File Offset: 0x00041D04
		private static string diskId()
		{
			return HWID.identifier("Win32_DiskDrive", "Model") + HWID.identifier("Win32_DiskDrive", "Manufacturer") + HWID.identifier("Win32_DiskDrive", "Signature") + HWID.identifier("Win32_DiskDrive", "TotalHeads");
		}

		// Token: 0x06000CB5 RID: 3253 RVA: 0x00043B7C File Offset: 0x00041D7C
		private static string baseId()
		{
			return HWID.identifier("Win32_BaseBoard", "Model") + HWID.identifier("Win32_BaseBoard", "Manufacturer") + HWID.identifier("Win32_BaseBoard", "Name") + HWID.identifier("Win32_BaseBoard", "SerialNumber");
		}

		// Token: 0x06000CB6 RID: 3254 RVA: 0x0000D8AA File Offset: 0x0000BAAA
		private static string macId()
		{
			return HWID.identifier("Win32_NetworkAdapterConfiguration", "MACAddress", "IPEnabled");
		}

		// Token: 0x040006E4 RID: 1764
		public static string fingerPrint = string.Empty;
	}
}
