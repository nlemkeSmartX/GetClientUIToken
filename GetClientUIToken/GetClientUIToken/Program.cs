using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Runtime.InteropServices;

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

        public static void Main(string[] args)
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

            var usernameInput = driver.FindElement(By.Id("Username"));
            usernameInput.SendKeys(settingsJson["Username"].ToString());

            var passwordInput = driver.FindElement(By.Id("Password"));
            passwordInput.SendKeys(settingsJson["Password"].ToString());

            var loginButton = driver.FindElement(By.Name("button"));
            loginButton.Click();


            var submitButton = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.XPath("//button[@type='submit']")));
            submitButton.Click();
            Console.WriteLine($"Getting Token");
            driver.Navigate().GoToUrl("https://qa-hedgecovest.pantheonsite.io/v3/dashboard");
            //wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.CssSelector("#main-content-wrap > div > div > div > div > div > div > div > div.col-xs-12.explore-component > div:nth-child(1) > div > button")));

            
            var js = (IJavaScriptExecutor)driver;
            var authJsonString = (String)js.ExecuteScript("return localStorage.getItem('oidc.user:https://ids.svc.qa.smartx.us/:smartx-clientapi-webclient')");
            var authJson = JObject.Parse(authJsonString);
            var token = authJson["access_token"].ToString();

            OpenClipboard(IntPtr.Zero);
            var ptr = Marshal.StringToHGlobalUni(token);
            SetClipboardData(13, ptr);
            CloseClipboard();
            Marshal.FreeHGlobal(ptr);
            
            driver.Quit();
            Console.Clear();

            Console.WriteLine($"Token: {token}");
            Console.WriteLine("User Token copied to clipboard");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}