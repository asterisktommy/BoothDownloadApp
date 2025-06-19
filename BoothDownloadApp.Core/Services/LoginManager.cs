using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace BoothDownloadApp
{
    public static class LoginManager
    {
        public static async Task<bool> IsLoggedInAsync()
        {
            CookieContainer cookies = CookieStore.Load();
            using HttpClientHandler handler = new HttpClientHandler { CookieContainer = cookies, AllowAutoRedirect = true };
            using HttpClient client = new HttpClient(handler);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                "AppleWebKit/537.36 (KHTML, like Gecko) " +
                "Chrome/123.0.0.0 Safari/537.36");
            try
            {
                using HttpResponseMessage response = await client.GetAsync("https://accounts.booth.pm/library");
                string finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? string.Empty;
                if (finalUrl.Contains("sign_in") || finalUrl.Contains("login"))
                {
                    return false;
                }
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
