using System.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinterspringLauncher;

public class UpdateApiClient
{
    private readonly LauncherConfig _config;

    public UpdateApiClient(LauncherConfig config)
    {
        _config = config;
    }

    public (string? provider, string downloadUrl) GetWindowsGameDownloadSource()
    {
        return _config.WindowsGameDownloadUrl == LauncherConfig.DEFAULT_CONFIG_VALUE
            ? ("azeroth-classic.org", "http://dl.azeroth-classic.org/clients/WoW_Classic_1.14.2.42597_All_Languages.rar")
            : (null, _config.WindowsGameDownloadUrl);
    }

    public (string? provider, string downloadUrl) GetMacGamePatchDownloadSource()
    {
        return _config.MacGameDownloadUrl == LauncherConfig.DEFAULT_CONFIG_VALUE
            ? ("wowdl.net", "https://download.wowdl.net/downloadFiles/Clients/WoW_Classic_1.14.2.42597_macOS.zip")
            : (null, _config.MacGameDownloadUrl);
    }

    public string GetGamePatchingServiceUrl()
    {
        return _config.GamePatcherUrl == LauncherConfig.DEFAULT_CONFIG_VALUE
            ? "https://wow-patching-service.blu.wtf/api/patch-game"
            : _config.GamePatcherUrl;
    }

    public GitHubReleaseInfo GetLatestThisLauncherRelease()
    {
        return GetGitHubReleaseInfo(_config.GitRepoWinterspringLauncher);
    }

    public GitHubReleaseInfo GetLatestHermesProxyRelease()
    {
        return GetGitHubReleaseInfo(_config.GitRepoHermesProxy);
    }

    public GitHubReleaseInfo GetLatestArctiumLauncherRelease()
    {
        return GetGitHubReleaseInfo(_config.GitRepoArctiumLauncher);
    }

    private static GitHubReleaseInfo GetGitHubReleaseInfo(string repo)
    {
        var releaseUrl = $"https://api.github.com/repos/{repo}/releases/latest";
        var releaseInfo = PerformWebRequest<GitHubReleaseInfo>(releaseUrl);
        return releaseInfo;
    }

    private static TJsonResponse PerformWebRequest<TJsonResponse>(string url) where TJsonResponse : new()
    {
        var proxy = new WebProxy("http://000000")
        {
            Credentials = new NetworkCredential("000000", "000000")
        };

        using var httpClientHandler = new HttpClientHandler
        {
            Proxy = proxy,
            UseProxy = true,
        };

        using var client = new HttpClient(httpClientHandler);
        client.DefaultRequestHeaders.Add("User-Agent", "curl/7.0.0"); // otherwise we get blocked
                                                                      // 添加 Authorization 头部，使用 Bearer 认证方式，并附加 Access Token
        string accessToken = "ghp_SCCE1kN3SEd8HLIFwvNGi3bfdzxRW11xQ48D";
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = client.GetAsync(url).GetAwaiter().GetResult();
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            if (response.ReasonPhrase == "rate limit exceeded")
            {
                Console.WriteLine("You are being rate-limited, did you open the launcher too many times in a short time?");
                return new TJsonResponse();
            }
        }
        response.EnsureSuccessStatusCode();
        var rawJson = response.Content.ReadAsStringAsync().GetAwaiter().GetResult(); // easier to debug with a string and the performance is neglectable for such small jsons
        var parsedJson = JsonSerializer.Deserialize<TJsonResponse>(rawJson);
        if (parsedJson == null)
        {
            Console.WriteLine($"Debug: {rawJson}");
            throw new NoNullAllowedException("The web response resulted in a null object");
        }
        return parsedJson;
    }
}

public class GitHubReleaseInfo
{
    [JsonPropertyName("name")] 
    public string? Name { get; set; }
    
    [JsonPropertyName("tag_name")] 
    public string? TagName { get; set; }

    [JsonPropertyName("assets")] 
    public List<Asset>? Assets { get; set; }

    public class Asset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("browser_download_url")]
        public string DownloadUrl { get; set; } = null!;
    }
}
