using System;
using System.Text;

namespace Hackus_Mail_Checker_Reforged.Net.Mail
{
	// Token: 0x020000FE RID: 254
	public static class EncodingHelper
	{
		// Token: 0x060007D4 RID: 2004 RVA: 0x0002F744 File Offset: 0x0002D944
		public static Encoding ParseEncodingName(string name)
		{
			name = EncodingHelper.CleanEncodingName(name);
			Encoding result;
			try
			{
				if (!name.Contains("ansi") && !name.Contains("ansix3"))
				{
					if (!name.Contains("cp932") && !name.Contains("shiftjis"))
					{
						if (!name.Contains("cp1250"))
						{
							if (!name.Contains("ascii"))
							{
								if (name.Contains("cp1252"))
								{
									result = Encoding.GetEncoding(1252);
								}
								else if (!name.Contains("utf-8") && !name.Contains("utf8"))
								{
									if (name.Contains("ksc5601-1987"))
									{
										result = Encoding.GetEncoding(949);
									}
									else if (name.Contains("cp-850"))
									{
										result = Encoding.GetEncoding(850);
									}
									else if (!name.Contains("cp936"))
									{
										if (name.Contains("cp874"))
										{
											result = Encoding.GetEncoding(874);
										}
										else if (!name.Contains("8859-15") && !name.Contains("885915"))
										{
											if (!name.Contains("8859-13") && !name.Contains("885913"))
											{
												if (!name.Contains("8859-9") && !name.Contains("88599"))
												{
													if (!name.Contains("8859-8") && !name.Contains("88598"))
													{
														if (!name.Contains("8859-7") && !name.Contains("88597"))
														{
															if (!name.Contains("8859-6") && !name.Contains("88596"))
															{
																if (!name.Contains("8859-5") && !name.Contains("88595"))
																{
																	if (!name.Contains("8859-4") && !name.Contains("88594"))
																	{
																		if (!name.Contains("8859-3") && !name.Contains("88593"))
																		{
																			if (!name.Contains("8859-2") && !name.Contains("88592"))
																			{
																				if (!name.Contains("8859-1") && !name.Contains("88591"))
																				{
																					result = Encoding.GetEncoding(name);
																				}
																				else
																				{
																					result = Encoding.GetEncoding(28591);
																				}
																			}
																			else
																			{
																				result = Encoding.GetEncoding(28592);
																			}
																		}
																		else
																		{
																			result = Encoding.GetEncoding(28593);
																		}
																	}
																	else
																	{
																		result = Encoding.GetEncoding(28594);
																	}
																}
																else
																{
																	result = Encoding.GetEncoding(28595);
																}
															}
															else
															{
																result = Encoding.GetEncoding(28596);
															}
														}
														else
														{
															result = Encoding.GetEncoding(28597);
														}
													}
													else
													{
														result = Encoding.GetEncoding(28598);
													}
												}
												else
												{
													result = Encoding.GetEncoding(28599);
												}
											}
											else
											{
												result = Encoding.GetEncoding(28603);
											}
										}
										else
										{
											result = Encoding.GetEncoding(28605);
										}
									}
									else
									{
										result = Encoding.GetEncoding(936);
									}
								}
								else
								{
									result = Encoding.UTF8;
								}
							}
							else
							{
								result = Encoding.ASCII;
							}
						}
						else
						{
							result = Encoding.GetEncoding(1250);
						}
					}
					else
					{
						result = Encoding.GetEncoding(932);
					}
				}
				else
				{
					result = Encoding.GetEncoding(20127);
				}
			}
			catch
			{
				result = Encoding.UTF8;
			}
			return result;
		}

		// Token: 0x060007D5 RID: 2005 RVA: 0x0002FB78 File Offset: 0x0002DD78
		private static string CleanEncodingName(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return string.Empty;
			}
			if (name.StartsWith("charset="))
			{
				name = name.Replace("charset=", "");
			}
			if (name.EndsWith("http-equivContent-Type"))
			{
				name = name.Replace("http-equivContent-Type", "");
			}
			foreach (string text in EncodingHelper._replacements)
			{
				if (name.Contains(text))
				{
					name.Replace(text, string.Empty);
				}
			}
			return name.ToLower();
		}

		// Token: 0x040003F3 RID: 1011
		private static readonly string[] _replacements = new string[]
		{
			"_",
			"$ESC",
			"'",
			"3d\"",
			"\"",
			" "
		};
	}
}
