using System.Text.Json;
using job_crawler.Models;

namespace job_crawler.Utils;

public static class ConfigLoader
{
    public static string BuildUrl(JobSiteConfig? config)
    {
        if (config.QueryParams == null || config.QueryParams.Count == 0)
            return config.BaseUrl;

        var query = string.Join("&", config.QueryParams.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        return $"{config.BaseUrl}?{query}";
    }

    public static Credentials LoadCredentials(string path)
    {
        var json = File.ReadAllText(GetRelativePath(path));
        return JsonSerializer.Deserialize<Credentials>(json);
    }

    private static string GetRelativePath(string path)
    {
        var basePath = AppContext.BaseDirectory;
        return Path.Combine(basePath, path);
    }
}