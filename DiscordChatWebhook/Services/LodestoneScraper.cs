using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace DiscordChatWebhook.Services;

public class LodestoneScraper
{
    private readonly HttpClient _http;
    private readonly ConcurrentDictionary<string, string> _cache = new();

    private const string _userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    public LodestoneScraper()
    {
        this._http = new HttpClient();
        this._http.DefaultRequestHeaders.UserAgent.ParseAdd(_userAgent);
    }

    public async Task<string> GetAvatarAsync(string name, string world)
    {
        if (string.IsNullOrEmpty(world) || string.IsNullOrEmpty(name)) return "";

        string key = $"{name}@{world}";
        if (this._cache.TryGetValue(key, out var cachedUrl)) return cachedUrl;

        try
        {
            string searchUrl = $"https://na.finalfantasyxiv.com/lodestone/character/?q={Uri.EscapeDataString(name)}&worldname={world}";
            string html = await this._http.GetStringAsync(searchUrl);

            string pattern = $@"<img src=""([^""]+)""[^>]*alt=""{Regex.Escape(name)}""";
            var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);

            if (match.Success)
            {
                string avatarUrl = match.Groups[1].Value;
                this._cache.TryAdd(key, avatarUrl);
                return avatarUrl;
            }

            Service.Logger.Debug($"[Lodestone] No avatar found for {key}");
        }
        catch (Exception ex)
        {
            Service.Logger.Error($"[Lodestone] Error scraping {key}: {ex.Message}");
        }

        return "";
    }
}