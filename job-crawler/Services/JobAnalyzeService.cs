using System.Web;
using job_crawler.Library;
using job_crawler.Models;

namespace job_crawler.Services;

public class JobAnalyzeService
{
    private readonly List<Keyword> _keywords;

    public JobAnalyzeService()
    {
        _keywords = KeywordLoader.LoadFromJson("Configs/keywords.json");
    }

    public void AnalyzeJob(Job job)
    {
        job.Score = 0;

        var titleKeywords = _keywords.Where(k => k.Type == Keyword.KeywordType.Title).ToList();
        var descKeywords = _keywords.Where(k => k.Type == Keyword.KeywordType.Description).ToList();

        // Analyze title
        Dictionary<string, int> titleWords = WordHandleLibrary.BreakdownWords(job.Title);
        var titleScore = WordHandleLibrary.AnalyzeWord(titleKeywords, titleWords);

        // Analyze description
        Dictionary<string, int> descWords = WordHandleLibrary.BreakdownWords(job.Description);
        var descScore = WordHandleLibrary.AnalyzeWord(descKeywords, descWords);
        job.Score = titleScore + descScore;
    }

    public static string GetJobIdFromUrl(string url)
    {
        StaticValue.JobSites site = GetJobSiteTypeFromUrl(url);
        var uri = new Uri(url);
        switch (site)
        {
            case StaticValue.JobSites.Indeed:
                var query = HttpUtility.ParseQueryString(uri.Query);
                return query["jk"]; // Extracts the 'jk' parameter value
            case StaticValue.JobSites.LinkedIn:
                var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                return (segments.Length >= 3 && segments[1] == "view") ? segments[2] : "";
            default:
                return null;
        }
    }

    public static StaticValue.JobSites GetJobSiteTypeFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) throw new ArgumentNullException("url");
        url = url.ToLower();
        StaticValue.JobSites site = url switch
        {
            var u when u.Contains("indeed", StringComparison.OrdinalIgnoreCase) => StaticValue.JobSites.Indeed,
            var u when u.Contains("linkedin", StringComparison.OrdinalIgnoreCase) => StaticValue.JobSites.LinkedIn,
            _ => throw new NotSupportedException("Unknown job site")
        };
        return site;
    }
}