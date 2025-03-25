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

    public string GetJobIdFromUrl(string url)
    {
        var uri = new Uri(url);
        var query = HttpUtility.ParseQueryString(uri.Query);
        return query["jk"]; // Extracts the 'jk' parameter value
    }
}