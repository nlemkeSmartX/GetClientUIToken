using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Web;

namespace GetClientUIToken
{
    public class Program
    {
        [DllImport("user32.dll")]
        internal static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        internal static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        internal static extern bool SetClipboardData(uint uFormat, IntPtr data);

        public static async Task Main(string[] args)
        {
            var settingsString = File.ReadAllText("settings.json");
            var settingsJson = JObject.Parse(settingsString);


            var options = new ChromeOptions();
            options.AddArguments("headless");
            options.AddArguments("window-size=1920,1080");
            options.AddArguments("--log-level=3");
            var driver = new ChromeDriver(options);
            Console.Clear();
            Console.WriteLine($"Logging In");

            driver.Navigate().GoToUrl("https://qa-hedgecovest.pantheonsite.io/v3/dashboard");
            var wait = new WebDriverWait(driver, new TimeSpan(0, 0, 30));
            wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.Id("Username")));

            //  Enter Login Info
            var usernameInput = driver.FindElement(By.Id("Username"));
            usernameInput.SendKeys(settingsJson["Username"].ToString());
            var passwordInput = driver.FindElement(By.Id("Password"));
            passwordInput.SendKeys(settingsJson["Password"].ToString());
            var loginButton = driver.FindElement(By.Name("button"));
            loginButton.Click();

            //  Skip mask as user
            var submitButton = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.XPath("//button[@type='submit']")));
            submitButton.Click();

            //  Strip out the access token from the callback url
            WebDriverWait wait2 = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
            wait.Until(driver => driver.Url.Contains("id_token"));
            var fullURLWithToken = HttpUtility.UrlDecode(driver.Url);
            string idTokenPattern = @"(?<=access_token=)[^&]+";
            Match match = Regex.Match(fullURLWithToken, idTokenPattern);
            var token = match.Groups[0].Value;
            Console.WriteLine($"Got Token");

            //  Copy the token to the clipboard
            OpenClipboard(IntPtr.Zero);
            var ptr = Marshal.StringToHGlobalUni(token);
            SetClipboardData(13, ptr);
            CloseClipboard();
            Marshal.FreeHGlobal(ptr);

            driver.Quit();
            Console.WriteLine($"Token: {token}");
            Console.WriteLine("User Token copied to clipboard");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}