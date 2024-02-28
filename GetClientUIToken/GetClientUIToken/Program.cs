using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;

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

        private static async Task<string> GetAccessTokenAsync()
        {
            var settingsString = File.ReadAllText("settings.json");
            var settingsJson = JObject.Parse(settingsString);
            

            using var client = new HttpClient();
            var url = "https://ids.svc.qa.smartx.us/connect/token";
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username", settingsJson["Username"].ToString()),
                new KeyValuePair<string, string>("password", settingsJson["Password"].ToString()),
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("client_id", settingsJson["ClientID"].ToString()),
                new KeyValuePair<string, string>("client_secret", settingsJson["Secret"].ToString()),
            });

            var response = await client.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();

            using var document = JsonDocument.Parse(responseString);

            // Access the root element
            var root = document.RootElement;

            // Check if the property exists
            if (root.TryGetProperty("access_token", out JsonElement tokenElement))
            {
                // Extract the string value
                string accessToken = tokenElement.GetString();
                return accessToken;
            }
            else
            {
                throw new Exception("Property 'access_token' not found in JSON.");
            }
        }

        public static async Task Main(string[] args)
        {
            var token = await GetAccessTokenAsync();

            //  Copy the token to the clipboard
            OpenClipboard(IntPtr.Zero);
            var ptr = Marshal.StringToHGlobalUni(token);
            SetClipboardData(13, ptr);
            CloseClipboard();
            Marshal.FreeHGlobal(ptr);

            Console.WriteLine($"Token: {token}");
            Console.WriteLine("User Token copied to clipboard");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}