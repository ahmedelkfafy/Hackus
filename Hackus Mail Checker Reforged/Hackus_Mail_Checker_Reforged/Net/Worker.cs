using System;
using System.Linq;

namespace Hackus_Mail_Checker_Reforged.Net
{
	// Token: 0x020000B7 RID: 183
	internal static class Worker
	{
		// Token: 0x060005D9 RID: 1497 RVA: 0x00027060 File Offset: 0x00025260
		public static void Run()
		{
			/*
An exception occurred when decompiling this method (060005D9)

ICSharpCode.Decompiler.DecompilerException: Error decompiling System.Void Hackus_Mail_Checker_Reforged.Net.Worker::Run()

 ---> System.NullReferenceException: Object reference not set to an instance of an object.
   at ICSharpCode.Decompiler.ILAst.ILAstOptimizer.IntroducePropertyAccessInstructions(ILExpression expr, ILExpression parentExpr, Int32 posInParent) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\ILAst\ILAstOptimizer.cs:line 1587
   at ICSharpCode.Decompiler.ILAst.ILAstOptimizer.IntroducePropertyAccessInstructions(ILNode node) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\ILAst\ILAstOptimizer.cs:line 1579
   at ICSharpCode.Decompiler.ILAst.ILAstOptimizer.Optimize(DecompilerContext context, ILBlock method, AutoPropertyProvider autoPropertyProvider, StateMachineKind& stateMachineKind, MethodDef& inlinedMethod, AsyncMethodDebugInfo& asyncInfo, ILAstOptimizationStep abortBeforeStep) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\ILAst\ILAstOptimizer.cs:line 244
   at ICSharpCode.Decompiler.Ast.AstMethodBodyBuilder.CreateMethodBody(IEnumerable`1 parameters, MethodDebugInfoBuilder& builder) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\Ast\AstMethodBodyBuilder.cs:line 123
   at ICSharpCode.Decompiler.Ast.AstMethodBodyBuilder.CreateMethodBody(MethodDef methodDef, DecompilerContext context, AutoPropertyProvider autoPropertyProvider, IEnumerable`1 parameters, Boolean valueParameterIsKeyword, StringBuilder sb, MethodDebugInfoBuilder& stmtsBuilder) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\Ast\AstMethodBodyBuilder.cs:line 88
   --- End of inner exception stack trace ---
   at ICSharpCode.Decompiler.Ast.AstMethodBodyBuilder.CreateMethodBody(MethodDef methodDef, DecompilerContext context, AutoPropertyProvider autoPropertyProvider, IEnumerable`1 parameters, Boolean valueParameterIsKeyword, StringBuilder sb, MethodDebugInfoBuilder& stmtsBuilder) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\Ast\AstMethodBodyBuilder.cs:line 92
   at ICSharpCode.Decompiler.Ast.AstBuilder.AddMethodBody(EntityDeclaration methodNode, EntityDeclaration& updatedNode, MethodDef method, IEnumerable`1 parameters, Boolean valueParameterIsKeyword, MethodKind methodKind) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\Ast\AstBuilder.cs:line 1533
*/;
		}

		// Token: 0x060005DA RID: 1498 RVA: 0x00027364 File Offset: 0x00025564
		private static bool IsBelongsTo(string[] domainsGroup, string domain)
		{
			return domainsGroup.Any((string d) => d.EqualsIgnoreCase(domain));
		}

		// Token: 0x0400030D RID: 781
		private static readonly string[] _onetDomains = new string[]
		{
			"op.pl",
			"vp.pl",
			"onet.pl",
			"onet.com.pl",
			"poczta.onet.pl",
			"opoczta.pl",
			"poczta.onet.eu",
			"poczta.onet.eu.pl",
			"autograf.pl",
			"buziaczek.pl",
			"spoko.pl",
			"amorki.pl"
		};

		// Token: 0x0400030E RID: 782
		private static readonly string[] _unitedInternetDomains = new string[]
		{
			"gmx.net",
			"gmx.de",
			"gmx.us",
			"gmx.at",
			"web.de",
			"gmx.com",
			"gmx.ch"
		};

		// Token: 0x0400030F RID: 783
		private static readonly string[] _mailRuDomains = new string[]
		{
			"mail.ru",
			"mail.ua",
			"bk.ru",
			"bk.ua",
			"list.ru",
			"list.ua",
			"inbox.ru",
			"internet.ru"
		};

		// Token: 0x04000310 RID: 784
		private static readonly string[] _yandexDomains = new string[]
		{
			"yandex.ru",
			"ya.ru",
			"sochi.com",
			"narod.ru"
		};

		// Token: 0x04000311 RID: 785
		private static readonly string[] _ramblerDomains = new string[]
		{
			"rambler.ru",
			"lenta.ru",
			"ro.ru",
			"myrambler.ru",
			"autorambler.ru"
		};

		// Token: 0x04000312 RID: 786
		private static readonly string[] _protonDomains = new string[]
		{
			"protonmail.com",
			"proton.me",
			"pm.me",
			"protonmail.ch"
		};

		// Token: 0x04000313 RID: 787
		private static readonly string[] _outlookDomains = new string[]
		{
			"outlook.com",
			"hotmail.com"
		};

		// Token: 0x04000314 RID: 788
		private static readonly string[] _interiaDomains = new string[]
		{
			"interia.pl",
			"poczta.fm",
			"interia.eu",
			"interia.com",
			"intmail.pl",
			"interiowy.pl",
			"adresik.net",
			"adresik.pl",
			"pisz.to",
			"vip.interia.pl",
			"pacz.to",
			"ogarnij.se"
		};

		// Token: 0x04000315 RID: 789
		private static readonly string[] _seznamDomains = new string[]
		{
			"post.cz",
			"seznam.cz",
			"email.cz",
			"spoluzaci.cz",
			"stream.cz"
		};
	}
}
