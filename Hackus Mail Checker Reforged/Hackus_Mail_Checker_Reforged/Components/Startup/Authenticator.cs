using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Hackus_Mail_Checker_Reforged.Services.Background;
using Newtonsoft.Json;
using xNet;

namespace Hackus_Mail_Checker_Reforged.Components.Startup
{
	// Token: 0x020001AD RID: 429
	internal static class Authenticator
	{
		// Token: 0x06000C8A RID: 3210 RVA: 0x00042A54 File Offset: 0x00040C54
		public static async Task Login(string username, string password)
		{
			try
			{
				EncryptionKeyResponse encryptionKeyResponse = await Authenticator.GetEncryptionKeyAsync(username);
				if (encryptionKeyResponse == null || !encryptionKeyResponse.IsValid())
				{
					return;
				}
				string hwid = await Authenticator.GetHWID();
				string serializedCredentials = JsonConvert.SerializeObject(new { username, password });
				string credentials = Authenticator.EncryptRsa(serializedCredentials, encryptionKeyResponse.Key);
				string loginResponseJson = await Authenticator.GetLoginResponseAsync(credentials, encryptionKeyResponse.Guid);
				if (loginResponseJson == null)
				{
					return;
				}
				LoginResponse loginResponse = Authenticator.DecryptLoginResponse(loginResponseJson, serializedCredentials);
				if (loginResponse != null && loginResponse.IsValid())
				{
					BackgroundAuthenticator.Instance.SetProperties(username, loginResponse.Token, loginResponse.Key, loginResponse.DatabasePath, loginResponse.DatabaseVersion);
				}
			}
			catch
			{
			}
		}

		// Token: 0x06000C8B RID: 3211 RVA: 0x00042AA0 File Offset: 0x00040CA0
		private static async Task<EncryptionKeyResponse> GetEncryptionKeyAsync(string username)
		{
			return await Task.Run(() => Authenticator.GetEncryptionKey(username));
		}

		// Token: 0x06000C8C RID: 3212 RVA: 0x00042AE4 File Offset: 0x00040CE4
		private static EncryptionKeyResponse GetEncryptionKey(string username)
		{
			/*
An exception occurred when decompiling this method (06000C8C)

ICSharpCode.Decompiler.DecompilerException: Error decompiling Hackus_Mail_Checker_Reforged.Components.Startup.EncryptionKeyResponse Hackus_Mail_Checker_Reforged.Components.Startup.Authenticator::GetEncryptionKey(System.String)

 ---> System.Exception: Inconsistent stack size at IL_A2
   at ICSharpCode.Decompiler.ILAst.ILAstBuilder.StackAnalysis(MethodDef methodDef) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\ILAst\ILAstBuilder.cs:line 443
   at ICSharpCode.Decompiler.ILAst.ILAstBuilder.Build(MethodDef methodDef, Boolean optimize, DecompilerContext context) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\ILAst\ILAstBuilder.cs:line 269
   at ICSharpCode.Decompiler.Ast.AstMethodBodyBuilder.CreateMethodBody(IEnumerable`1 parameters, MethodDebugInfoBuilder& builder) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\Ast\AstMethodBodyBuilder.cs:line 112
   at ICSharpCode.Decompiler.Ast.AstMethodBodyBuilder.CreateMethodBody(MethodDef methodDef, DecompilerContext context, AutoPropertyProvider autoPropertyProvider, IEnumerable`1 parameters, Boolean valueParameterIsKeyword, StringBuilder sb, MethodDebugInfoBuilder& stmtsBuilder) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\Ast\AstMethodBodyBuilder.cs:line 88
   --- End of inner exception stack trace ---
   at ICSharpCode.Decompiler.Ast.AstMethodBodyBuilder.CreateMethodBody(MethodDef methodDef, DecompilerContext context, AutoPropertyProvider autoPropertyProvider, IEnumerable`1 parameters, Boolean valueParameterIsKeyword, StringBuilder sb, MethodDebugInfoBuilder& stmtsBuilder) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\Ast\AstMethodBodyBuilder.cs:line 92
   at ICSharpCode.Decompiler.Ast.AstBuilder.AddMethodBody(EntityDeclaration methodNode, EntityDeclaration& updatedNode, MethodDef method, IEnumerable`1 parameters, Boolean valueParameterIsKeyword, MethodKind methodKind) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\Ast\AstBuilder.cs:line 1533
*/;
		}

		// Token: 0x06000C8D RID: 3213 RVA: 0x00042C14 File Offset: 0x00040E14
		private static async Task<string> GetLoginResponseAsync(string credentials, string guid)
		{
			return await Task.Run(() => Authenticator.GetLoginResponse(credentials, guid));
		}

		// Token: 0x06000C8E RID: 3214 RVA: 0x00042C60 File Offset: 0x00040E60
		private static string GetLoginResponse(string credentials, string guid)
		{
			/*
An exception occurred when decompiling this method (06000C8E)

ICSharpCode.Decompiler.DecompilerException: Error decompiling System.String Hackus_Mail_Checker_Reforged.Components.Startup.Authenticator::GetLoginResponse(System.String,System.String)

 ---> System.Exception: Inconsistent stack size at IL_D1
   at ICSharpCode.Decompiler.ILAst.ILAstBuilder.StackAnalysis(MethodDef methodDef) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\ILAst\ILAstBuilder.cs:line 443
   at ICSharpCode.Decompiler.ILAst.ILAstBuilder.Build(MethodDef methodDef, Boolean optimize, DecompilerContext context) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\ILAst\ILAstBuilder.cs:line 269
   at ICSharpCode.Decompiler.Ast.AstMethodBodyBuilder.CreateMethodBody(IEnumerable`1 parameters, MethodDebugInfoBuilder& builder) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\Ast\AstMethodBodyBuilder.cs:line 112
   at ICSharpCode.Decompiler.Ast.AstMethodBodyBuilder.CreateMethodBody(MethodDef methodDef, DecompilerContext context, AutoPropertyProvider autoPropertyProvider, IEnumerable`1 parameters, Boolean valueParameterIsKeyword, StringBuilder sb, MethodDebugInfoBuilder& stmtsBuilder) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\Ast\AstMethodBodyBuilder.cs:line 88
   --- End of inner exception stack trace ---
   at ICSharpCode.Decompiler.Ast.AstMethodBodyBuilder.CreateMethodBody(MethodDef methodDef, DecompilerContext context, AutoPropertyProvider autoPropertyProvider, IEnumerable`1 parameters, Boolean valueParameterIsKeyword, StringBuilder sb, MethodDebugInfoBuilder& stmtsBuilder) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\Ast\AstMethodBodyBuilder.cs:line 92
   at ICSharpCode.Decompiler.Ast.AstBuilder.AddMethodBody(EntityDeclaration methodNode, EntityDeclaration& updatedNode, MethodDef method, IEnumerable`1 parameters, Boolean valueParameterIsKeyword, MethodKind methodKind) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\Ast\AstBuilder.cs:line 1533
*/;
		}

		// Token: 0x06000C8F RID: 3215 RVA: 0x00042DBC File Offset: 0x00040FBC
		private static LoginResponse DecryptLoginResponse(string encryptedString, string serializedCredentials)
		{
			/*
An exception occurred when decompiling this method (06000C8F)

ICSharpCode.Decompiler.DecompilerException: Error decompiling Hackus_Mail_Checker_Reforged.Components.Startup.LoginResponse Hackus_Mail_Checker_Reforged.Components.Startup.Authenticator::DecryptLoginResponse(System.String,System.String)

 ---> System.Exception: Inconsistent stack size at IL_20
   at ICSharpCode.Decompiler.ILAst.ILAstBuilder.StackAnalysis(MethodDef methodDef) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\ILAst\ILAstBuilder.cs:line 443
   at ICSharpCode.Decompiler.ILAst.ILAstBuilder.Build(MethodDef methodDef, Boolean optimize, DecompilerContext context) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\ILAst\ILAstBuilder.cs:line 269
   at ICSharpCode.Decompiler.Ast.AstMethodBodyBuilder.CreateMethodBody(IEnumerable`1 parameters, MethodDebugInfoBuilder& builder) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\Ast\AstMethodBodyBuilder.cs:line 112
   at ICSharpCode.Decompiler.Ast.AstMethodBodyBuilder.CreateMethodBody(MethodDef methodDef, DecompilerContext context, AutoPropertyProvider autoPropertyProvider, IEnumerable`1 parameters, Boolean valueParameterIsKeyword, StringBuilder sb, MethodDebugInfoBuilder& stmtsBuilder) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\Ast\AstMethodBodyBuilder.cs:line 88
   --- End of inner exception stack trace ---
   at ICSharpCode.Decompiler.Ast.AstMethodBodyBuilder.CreateMethodBody(MethodDef methodDef, DecompilerContext context, AutoPropertyProvider autoPropertyProvider, IEnumerable`1 parameters, Boolean valueParameterIsKeyword, StringBuilder sb, MethodDebugInfoBuilder& stmtsBuilder) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\Ast\AstMethodBodyBuilder.cs:line 92
   at ICSharpCode.Decompiler.Ast.AstBuilder.AddMethodBody(EntityDeclaration methodNode, EntityDeclaration& updatedNode, MethodDef method, IEnumerable`1 parameters, Boolean valueParameterIsKeyword, MethodKind methodKind) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\Ast\AstBuilder.cs:line 1533
*/;
		}

		// Token: 0x06000C90 RID: 3216 RVA: 0x00042DFC File Offset: 0x00040FFC
		private static async Task<string> GetHWID()
		{
			return await Task.Run(() => HWID.Value());
		}

		// Token: 0x06000C91 RID: 3217 RVA: 0x00042E38 File Offset: 0x00041038
		public static async Task<VersionResponse> GetLastVersionAsync()
		{
			return await Task.Run(() => Authenticator.GetLastVersion());
		}

		// Token: 0x06000C92 RID: 3218 RVA: 0x00042E74 File Offset: 0x00041074
		public static VersionResponse GetLastVersion()
		{
			Version version = Assembly.GetExecutingAssembly().GetName().Version;
			string text = string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
			try
			{
				using (HttpRequest httpRequest = new HttpRequest())
				{
					httpRequest.IgnoreProtocolErrors = true;
					httpRequest.Authorization = BackgroundAuthenticator.Instance.Token;
					httpRequest.AddUrlParam("version", text);
					HttpResponse httpResponse = httpRequest.Get(new Uri(BackgroundAuthenticator.Instance.BaseUri, "api/static/checkUpdate"), null);
					string text2 = httpResponse.ToString();
					if (httpResponse.StatusCode == 200)
					{
						VersionResponse versionResponse = JsonConvert.DeserializeObject<VersionResponse>(text2);
						if (versionResponse != null)
						{
							return versionResponse;
						}
					}
				}
			}
			catch
			{
			}
			return new VersionResponse
			{
				IsLastVersion = true
			};
		}

		// Token: 0x06000C93 RID: 3219 RVA: 0x00042F74 File Offset: 0x00041174
		private static string EncryptRsa(string message, string key)
		{
			RSACryptoServiceProvider rsacryptoServiceProvider = new RSACryptoServiceProvider(4096);
			StringReader textReader = new StringReader(key);
			RSAParameters parameters = (RSAParameters)new XmlSerializer(typeof(RSAParameters)).Deserialize(textReader);
			rsacryptoServiceProvider.ImportParameters(parameters);
			byte[] bytes = Encoding.Unicode.GetBytes(message);
			return Convert.ToBase64String(rsacryptoServiceProvider.Encrypt(bytes, false));
		}

		// Token: 0x06000C94 RID: 3220 RVA: 0x00042FCC File Offset: 0x000411CC
		public static string EncryptAes(string message, string key, string IV = "H9CMLkbATaXW8Sgu")
		{
			byte[] key2 = Authenticator.CreateMD5(key);
			string result;
			using (Aes aes = Aes.Create())
			{
				aes.KeySize = 128;
				aes.BlockSize = 128;
				aes.Padding = PaddingMode.ISO10126;
				aes.Key = key2;
				aes.IV = Encoding.ASCII.GetBytes(IV);
				ICryptoTransform cryptoTransform = aes.CreateEncryptor();
				byte[] bytes = Encoding.UTF8.GetBytes(message);
				result = Convert.ToBase64String(cryptoTransform.TransformFinalBlock(bytes, 0, bytes.Length));
			}
			return result;
		}

		// Token: 0x06000C95 RID: 3221 RVA: 0x0004305C File Offset: 0x0004125C
		public static string DecryptAes(string message, string key, string IV = "H9CMLkbATaXW8Sgu")
		{
			byte[] buffer = Convert.FromBase64String(message);
			byte[] key2 = Authenticator.CreateMD5(key);
			string result;
			using (Aes aes = Aes.Create())
			{
				aes.KeySize = 128;
				aes.BlockSize = 128;
				aes.Padding = PaddingMode.ISO10126;
				aes.Mode = CipherMode.CBC;
				aes.Key = key2;
				aes.IV = Encoding.ASCII.GetBytes(IV);
				ICryptoTransform transform = aes.CreateDecryptor();
				using (MemoryStream memoryStream = new MemoryStream(buffer))
				{
					using (CryptoStream cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Read))
					{
						using (StreamReader streamReader = new StreamReader(cryptoStream))
						{
							result = streamReader.ReadToEnd();
						}
					}
				}
			}
			return result;
		}

		// Token: 0x06000C96 RID: 3222 RVA: 0x0004314C File Offset: 0x0004134C
		private static byte[] CreateMD5(string message)
		{
			byte[] result;
			using (MD5 md = MD5.Create())
			{
				byte[] bytes = Encoding.ASCII.GetBytes(message);
				result = md.ComputeHash(bytes);
			}
			return result;
		}
	}
}
