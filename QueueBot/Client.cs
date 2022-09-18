using Discord.Rest;
using Discord.WebSocket;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QueueBot
{
    public delegate void OnLoggedInEvent(object sender, Client client);
    public delegate void OnQRCodeChangedEvent(object sender, Client client);
    public delegate void OnQRCodeScanned(object sender, Client client, string avatar_url);
    public delegate void OnTimeoutEvent(object sender, Client client);
    public class Client
    {
        public string UserIP = null;
        public string SessionID = null;
        public string Token = null;
        public string CurrentQRCode = null;
        public RestUserMessage Message = null;
        public DiscordSocketClient Discord = null;
        public IWebDriver Driver = null;
        public ParallelOptions ParallelOptions = null;
        public ChromeOptions ChromeOptions = new ChromeOptions();
        public ChromeDriverService ChromeDriverSerive = ChromeDriverService.CreateDefaultService(Environment.CurrentDirectory);
        public event OnLoggedInEvent OnLoggedIn;
        public event OnQRCodeChangedEvent OnQRCodeChanged;
        public event OnQRCodeScanned OnQRCodeScanned;
        public event OnTimeoutEvent OnTimeout;

        public Client()
        {

        }

        public void URLChecker()
        {
            DateTime startTime = DateTime.Now;
            string lastURL = "";
            string lastQRCode = "";
            while (true)
            {
                try
                {
                    if (OnQRCodeChanged != null)
                    {
                        try { CurrentQRCode = Driver.FindElement(By.XPath(SettingsLoader.CurrentSettings.QrCodeOffset)).GetAttribute("src"); } catch { }

                        if (lastQRCode != CurrentQRCode)
                        {
                            Console.WriteLine("new qr");
                            OnQRCodeChanged.Invoke(this, this);
                            lastQRCode = CurrentQRCode;
                        }
                    }

                    if (OnQRCodeScanned != null)
                    {
                        Console.WriteLine("checking avatar");
                        string avatar_url = string.Empty;
                        try { avatar_url = Driver.FindElement(By.XPath(SettingsLoader.CurrentSettings.AvatarUrlOffset)).GetAttribute("src"); } catch { }
                        if (!string.IsNullOrWhiteSpace(avatar_url))
                        {
                            Console.WriteLine("found avatar");
                            Console.WriteLine(avatar_url);
                            OnQRCodeScanned.Invoke(this, this, avatar_url);
                        }
                    }

                    if (OnLoggedIn != null && Driver.Url != lastURL)
                    {
                        Console.WriteLine("checking url");
                        if (!Driver.Url.Contains("discord.com/login"))
                            OnLoggedIn.Invoke(this, this);

                        lastURL = Driver.Url;
                    }

                    if (startTime + TimeSpan.FromSeconds(SettingsLoader.CurrentSettings.QRCodeTimeout) <= DateTime.Now)
                    {
                        OnTimeout.Invoke(this, this);
                        Driver.Quit();
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    break;
                }
                Thread.Sleep(1000);
            }
        }
    }

    public static class ExtenstionMethods
    {
        public static void Initialize(this Client client)
        {
            client.ChromeOptions.SetLoggingPreference(LogType.Browser, LogLevel.All);
            client.ChromeOptions.SetLoggingPreference(LogType.Client, LogLevel.All);
            client.ChromeOptions.SetLoggingPreference(LogType.Driver, LogLevel.All);
            client.ChromeOptions.SetLoggingPreference(LogType.Profiler, LogLevel.All);
            client.ChromeOptions.SetLoggingPreference(LogType.Server, LogLevel.All);
            client.ChromeOptions.AddArgument("--disable-extensions");
            client.ChromeOptions.AddArgument("--incognito");
            if (SettingsLoader.CurrentSettings.HeadlessDriver)
                client.ChromeOptions.AddArgument("--headless");

            client.ChromeDriverSerive.SuppressInitialDiagnosticInformation = false;
            client.ChromeDriverSerive.HideCommandPromptWindow = true;

            client.Driver = new ChromeDriver(client.ChromeDriverSerive, client.ChromeOptions);
            client.Discord = new DiscordSocketClient();

            new Thread(() => client.URLChecker()).Start();
        }

        public static string GetQRCode(this Client client)
        {
            if (client.Driver == null)
                throw new Exception("Client has not been initialized");

            client.Driver.Navigate().GoToUrl("https://discord.com/login");

            for (int i = 0; i < 100; i++)
            {
                try
                {
                    client.CurrentQRCode = client.Driver.FindElement(By.XPath(SettingsLoader.CurrentSettings.QrCodeOffset)).GetAttribute("src");
                    return client.CurrentQRCode;
                }
                catch
                {
                    Thread.Sleep(500);
                }
            }

            return "TIMED OUT";
        }
    }
}