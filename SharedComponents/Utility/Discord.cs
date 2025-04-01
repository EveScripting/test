using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Newtonsoft.Json;

namespace SharedComponents.Utility
{
    public class Discord
    {
        public static async Task SendDiscordWebhook(string url, string content, string usernameOverride = null)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    var s = JsonConvert.SerializeObject(new { content = content, username = usernameOverride });
                    s = s.Replace(Environment.NewLine, "\\n");
                    
                    Debug.WriteLine(s);
                    StringContent httpContent = new StringContent(s, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await httpClient.PostAsync(url, httpContent);

                    if (response.IsSuccessStatusCode)
                    {
                        Debug.WriteLine("Webhook message sent successfully!");
                    }
                    else
                    {
                        Debug.WriteLine($"Failed to send webhook message. Status Code: {response.StatusCode}");
                    }
                }
                catch (HttpRequestException ex)
                {
                    Debug.WriteLine($"Error sending webhook message: {ex.Message}");
                }
            }
        }
    }
}
