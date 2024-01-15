using System.Text;
using System.Text.Json;
using AetherBox.Helpers.Faloop.Model;

namespace AetherBox.Helpers.Faloop
{
    public class FaloopApiClient : IDisposable
    {
        private readonly HttpClient client = new HttpClient();

        public async Task<UserRefreshResponse?> RefreshAsync()
        {
            HttpRequestMessage request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://faloop.app/api/auth/user/refresh"),
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    sessionId = (string)null
                }), Encoding.UTF8, "application/json"),
                Headers =
                {
                    { "Origin", "https://faloop.app" },
                    { "Referer", "https://faloop.app/" },
                    { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36" }
                }
            };
            UserRefreshResponse result = default(UserRefreshResponse);
            using (HttpResponseMessage response = await client.SendAsync(request))
            {
                Stream stream = await response.Content.ReadAsStreamAsync();
                int num = 0;
                UserRefreshResponse userRefreshResponse = default(UserRefreshResponse);
                try
                {
                    userRefreshResponse = await JsonSerializer.DeserializeAsync<UserRefreshResponse>(stream, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    num = 1;
                }
                catch (JsonException jsonException)
                {
                    throw jsonException; // Throw the JsonException explicitly
                }
                if (stream != null)
                {
                    await ((IAsyncDisposable)stream).DisposeAsync();
                }
                if (num == 1)
                {
                    result = userRefreshResponse;
                    return result;
                }
            }
            return result;
        }

        public async Task<UserLoginResponse?> LoginAsync(string username, string password, string sessionId, string token)
        {
            HttpRequestMessage request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://faloop.app/api/auth/user/login"),
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    username = username,
                    password = password,
                    rememberMe = false,
                    sessionId = sessionId
                }), Encoding.UTF8, "application/json"),
                Headers =
                {
                    { "Authorization", token },
                    { "Origin", "https://faloop.app" },
                    { "Referer", "https://faloop.app/login/" },
                    { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36" }
                }
            };
            UserLoginResponse result = default(UserLoginResponse);
            using (HttpResponseMessage response = await client.SendAsync(request))
            {
                Stream stream = await response.Content.ReadAsStreamAsync();
                int num = 0;
                UserLoginResponse userLoginResponse = default(UserLoginResponse);
                try
                {
                    userLoginResponse = await JsonSerializer.DeserializeAsync<UserLoginResponse>(stream, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    num = 1;
                }
                catch (JsonException jsonException)
                {
                    throw jsonException; // Throw the JsonException explicitly
                }
                if (stream != null)
                {
                    await ((IAsyncDisposable)stream).DisposeAsync();
                }
                if (num == 1)
                {
                    result = userLoginResponse;
                    return result;
                }
            }
            return result;
        }

        public async Task<string> DownloadText(Uri uri)
        {
            HttpRequestMessage request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = uri,
                Headers =
                {
                    { "Origin", "https://faloop.app" },
                    { "Referer", "https://faloop.app/" },
                    { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36" }
                }
            };
            using HttpResponseMessage response = await client.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }

        public void Dispose()
        {
            client.Dispose();
        }
    }
}
