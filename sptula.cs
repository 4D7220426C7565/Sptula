using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PuppeteerSharp;

partial class Program
{
    private static readonly string[] PuppeteerArgs =
    [
        "--disable-extensions",
        "--disable-gpu",
        "--no-sandbox"
    ];

    static readonly HashSet<string> allEmails = new();

    static async Task Main(string[] args)
    {
        Banner.Show();

        /* --- *SECTION - Ask Proxy --- */
        Class_Color.Console_Colors.WriteWithColor("\nDo you want to use a proxy?: ", ConsoleColor.White);
        string? useProxy = Console.ReadLine()?.ToLower();

        string? proxy = null;

        if (useProxy == "y")
        {
            Class_Color.Console_Colors.WriteWithColor("Enter proxy (format: type://host:port, e.g., socks5://127.0.0.1:1080): ", ConsoleColor.White);
            proxy = Console.ReadLine();

            if (string.IsNullOrEmpty(proxy))
            {
                Class_Color.Console_Colors.WriteWithColor("Invalid proxy settings.", ConsoleColor.Red);
                return;
            }

            bool isProxyValid = await IsProxyValidAsync(proxy);

            if (!isProxyValid)
            {
                Class_Color.Console_Colors.WriteWithColor("The proxy is not valid or not operational.", ConsoleColor.Yellow);
                return;
            }
        }

        /* --- *NOTE - Scan from file --- */
        Class_Color.Console_Colors.WriteWithColor("\nDo you want to use URLs from a file? (y/n): ", ConsoleColor.White);
        string? useFile = Console.ReadLine()?.ToLower();

        List<string> urls = [];
        if (useFile == "y")
        {
            Class_Color.Console_Colors.WriteWithColor("Enter the file path: ", ConsoleColor.White);
            string? filePath = Console.ReadLine();
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                urls.AddRange(File.ReadAllLines(filePath));
            }
            else
            {
                Class_Color.Console_Colors.WriteWithColor("File not found or invalid path.", ConsoleColor.Red);
                return;
            }
        }
        else
        {
            Class_Color.Console_Colors.WriteWithColor("\nEnter url: ", ConsoleColor.White);
            string? url = Console.ReadLine();
            if (!string.IsNullOrEmpty(url))
            {
                urls.Add(url);
            }
            else
            {
                Class_Color.Console_Colors.WriteWithColor("The URI is empty.", ConsoleColor.Yellow);
                return;
            }
        }

        Class_Color.Console_Colors.WriteWithColor("Enter the limit: ", ConsoleColor.White);
        if (!int.TryParse(Console.ReadLine(), out int limit))
        {
            Class_Color.Console_Colors.WriteLineWithColor("Invalid limit, default: 15.", ConsoleColor.Blue);
            limit = 15;
        }

        string chromePath = @"./../../../../../usr/bin/chromium";
        if (!File.Exists(chromePath))
        {
            Class_Color.Console_Colors.WriteLineWithColor($"Chromium not found at {chromePath}", ConsoleColor.Red);
            return;
        }

        var argsList = PuppeteerArgs.ToList();

        /* --- Add proxy if needed --- */
        if (useProxy == "y" && !string.IsNullOrEmpty(proxy))
        {
            argsList.Add($"--proxy-server={proxy}");
        }

        var launchOptions = new LaunchOptions
        {
            Headless = true,
            ExecutablePath = chromePath,
            Args = [.. argsList] // Convert argument list back to an array
        };

        var browser = await Puppeteer.LaunchAsync(launchOptions);

        try
        {
            foreach (string url in urls)
            {
                var visitedUrls = new HashSet<string>();
                var queue = new Queue<string>();
                queue.Enqueue(url);
                string baseDomain = new Uri(url).Host;

                Console.CancelKeyPress += async (sender, e) =>
                {
                    e.Cancel = true;
                    ShowEmailsFound();
                    await browser.CloseAsync();
                };

                int visitedCount = 0;

                while (queue.Count > 0 && visitedCount < limit)
                {
                    string? currentUrl = queue.Dequeue();

                    if (visitedUrls.Contains(currentUrl))
                        continue;

                    visitedUrls.Add(currentUrl);

                    Class_Color.Console_Colors.WriteLineWithColor($"Navigating to: {currentUrl}", ConsoleColor.Cyan);
                    visitedCount++;

                    var page = await browser.NewPageAsync();
                    await page.SetRequestInterceptionAsync(true);
                    page.Request += async (sender, e) =>
                    {
                        if (e.Request.ResourceType == ResourceType.Image ||
                            e.Request.ResourceType == ResourceType.StyleSheet ||
                            e.Request.ResourceType == ResourceType.Font)
                        {
                            await e.Request.AbortAsync();
                        }
                        else
                        {
                            await e.Request.ContinueAsync();
                        }
                    };

                    try
                    {
                        var navigationOptions = new NavigationOptions
                        {
                            Timeout = 29000,
                            WaitUntil = [ WaitUntilNavigation.Networkidle2 ]
                        };

                        await page.GoToAsync(currentUrl, navigationOptions);

                        string pageSource = await page.GetContentAsync();
                        List<string> emails = ExtractEmails(pageSource);

                        foreach (string email in emails)
                        {
                            allEmails.Add(email);
                        }

                        var hiddenEmails = await page.EvaluateFunctionAsync<string[]>(@"
                            () => {
                                let emails = [];
                                let emailRegex = /([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})/g;
                                let elements = document.getElementsByTagName('*');
                                for (let element of elements) {
                                    if (element.textContent.match(emailRegex)) {
                                        let matches = element.textContent.match(emailRegex);
                                        emails.push(...matches);
                                    }
                                }
                                return emails;
                            }
                        ");

                        foreach (string hiddenEmail in hiddenEmails)
                        {
                            allEmails.Add(hiddenEmail);
                        }

                        var links = await page.EvaluateExpressionAsync<string[]>("Array.from(document.querySelectorAll('a[href]')).map(a => a.href)");

                        foreach (string link in links)
                        {
                            if (Uri.TryCreate(link, UriKind.Absolute, out Uri? uri) &&
                                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
                                IsSameDomain(baseDomain, uri.Host))
                            {
                                string normalizedUrl = uri.GetLeftPart(UriPartial.Path);
                                if (!visitedUrls.Contains(normalizedUrl))
                                {
                                    queue.Enqueue(normalizedUrl);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Class_Color.Console_Colors.WriteLineWithColor($"\nAn error occurred while navigating to {currentUrl}: {ex.Message}", ConsoleColor.Red);
                    }
                    finally
                    {
                        await page.CloseAsync();
                    }
                }
            }

            ShowEmailsFound();

            /* --- Save Emails --- */
            if (allEmails.Count > 0)
            {
                Class_Color.Console_Colors.WriteWithColor("Do you want to save the emails to a file? (y/n): ", ConsoleColor.Blue);
                string? saveToFile = Console.ReadLine()?.ToLower();
                if (saveToFile == "y")
                {
                    Class_Color.Console_Colors.WriteWithColor("Enter the file path: ", ConsoleColor.White);
                    string? filePath = Console.ReadLine();
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        EmailStorage.SaveEmailsToFile(allEmails, filePath);
                    }
                    else
                    {
                        Class_Color.Console_Colors.WriteWithColor("Invalid file path.", ConsoleColor.Yellow);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Class_Color.Console_Colors.WriteLineWithColor($"\nAn error occurred: {ex.Message}", ConsoleColor.DarkRed);
        }
        finally
        {
            await browser.CloseAsync();
        }
    }

    static async Task<bool> IsProxyValidAsync(string proxy)
    {
        try
        {
            var handler = new HttpClientHandler
            {
                Proxy = new WebProxy(proxy),
                UseProxy = true
            };

            using var httpClient = new HttpClient(handler);
            httpClient.Timeout = TimeSpan.FromSeconds(10); // Time of waiting 10s

            // Hacemos una solicitud a un sitio web conocido
            var response = await httpClient.GetAsync("http://www.google.com");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    static List<string> ExtractEmails(string input)
    {
        var emails = new List<string>();
        string pattern = @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b";

        MatchCollection matches = Regex.Matches(input, pattern);
        foreach (Match match in matches)
        {
            // Check if the match does not end with a file extension
            if (!MyRegex().IsMatch(match.Value))
            {
                emails.Add(match.Value);
            }
        }
        return emails;
    }

    static void ShowEmailsFound()
    {
        if (allEmails.Count > 0)
        {
            Class_Color.Console_Colors.WriteLineWithColor("\nEmails Found:", ConsoleColor.White);
            foreach (string email in allEmails)
            {
                Class_Color.Console_Colors.WriteLineWithColor($"{email}", ConsoleColor.DarkMagenta);
            }
            Class_Color.Console_Colors.WriteLineWithColor($"\nTotal Emails Found: {allEmails.Count}", ConsoleColor.White);
        }
        else
        {
            Class_Color.Console_Colors.WriteLineWithColor("\nNo emails found.", ConsoleColor.Blue);
        }
    }

    static bool IsSameDomain(string baseDomain, string host)
    {
        return host.EndsWith(baseDomain) || (baseDomain.StartsWith("www.") && host.EndsWith(baseDomain[4..]));
    }

    [GeneratedRegex(@"\.(png|webp|jpg|jpeg|gif|pdf|docx|xlsx)$", RegexOptions.IgnoreCase, "")]
    private static partial Regex MyRegex();
}
