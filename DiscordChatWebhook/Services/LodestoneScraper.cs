using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordChatWebhook.Services;

public partial class LodestoneScraper
{
    private record CacheEntry(string Url, DateTime ExpiresAt);

    private readonly HttpClient _http;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    private static readonly TimeSpan SuccessTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan FailureTtl = TimeSpan.FromMinutes(15);

    private const string _userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    public LodestoneScraper()
    {
        this._http = new HttpClient();
        this._http.DefaultRequestHeaders.UserAgent.ParseAdd(_userAgent);
    }

    [GeneratedRegex(@"<img src=""([^""]+)""[^>]*alt=""([^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex AvatarPattern();

    public async Task<string> GetAvatarAsync(string name, string world)
    {
        if (string.IsNullOrEmpty(world) || string.IsNullOrEmpty(name)) return "";

        string key = $"{name}@{world}";
        if (this._cache.TryGetValue(key, out var cached))
        {
            if (DateTime.UtcNow < cached.ExpiresAt)
                return cached.Url;
            this._cache.TryRemove(key, out _);
        }

        try
        {
            string searchUrl = $"https://na.finalfantasyxiv.com/lodestone/character/?q={Uri.EscapeDataString(name)}&worldname={world}";
            string html = await this._http.GetStringAsync(searchUrl);

            var match = AvatarPattern().Match(html);
            while (match.Success)
            {
                if (string.Equals(match.Groups[2].Value, name, StringComparison.OrdinalIgnoreCase))
                {
                    string avatarUrl = match.Groups[1].Value;
                    this._cache[key] = new CacheEntry(avatarUrl, DateTime.UtcNow + SuccessTtl);
                    return avatarUrl;
                }
                match = match.NextMatch();
            }

            Service.Logger.Debug($"[Lodestone] No avatar found for {key}");
        }
        catch (Exception ex)
        {
            Service.Logger.Error($"[Lodestone] Error scraping {key}: {ex.Message}");
        }

        // Cache failures to avoid repeated HTTP requests
        this._cache[key] = new CacheEntry("", DateTime.UtcNow + FailureTtl);
        return "";
    }
}
