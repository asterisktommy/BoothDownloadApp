using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace BoothDownloadApp
{
    public static class CookieStore
    {
        private static readonly string CookieFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "booth_cookies.json");

        public static async Task SaveAsync(IEnumerable<Cookie> cookies)
        {
            try
            {
                var list = cookies.Select(c => new SerializableCookie
                {
                    Name = c.Name,
                    Value = c.Value,
                    Domain = c.Domain,
                    Path = c.Path,
                    Expires = c.Expires == DateTime.MinValue ? null : c.Expires
                }).ToList();
                string json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(CookieFilePath, json);
            }
            catch { }
        }

        public static CookieContainer Load()
        {
            CookieContainer container = new CookieContainer();
            try
            {
                if (File.Exists(CookieFilePath))
                {
                    string json = File.ReadAllText(CookieFilePath);
                    var list = JsonSerializer.Deserialize<List<SerializableCookie>>(json);
                    if (list != null)
                    {
                        foreach (var c in list)
                        {
                            var cookie = new Cookie(c.Name, c.Value, c.Path ?? "/", c.Domain ?? "");
                            if (c.Expires.HasValue)
                            {
                                cookie.Expires = c.Expires.Value;
                            }
                            container.Add(cookie);
                        }
                    }
                }
            }
            catch { }
            return container;
        }

        private class SerializableCookie
        {
            public string Name { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
            public string Domain { get; set; } = string.Empty;
            public string Path { get; set; } = "/";
            public DateTime? Expires { get; set; }
        }
    }
}
