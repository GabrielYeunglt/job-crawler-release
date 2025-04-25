using job_crawler.Library;
using job_crawler.Models;
using OpenQA.Selenium;

namespace job_crawler.Parsers;

public abstract class JobSiteParser : IJobSiteParser
{
    public abstract string StartUrl { get; init; }
    public abstract string SiteName { get; init; }
    public abstract (int, int) WaitTimeRange { get; init; }
    public abstract StaticValue.JobSites Site { get; init; }
    public abstract List<Job> ExtractJobs(IWebDriver driver, HashSet<string> jobIds);
    public abstract bool TryGetNextPageUrl(IWebDriver driver, out string nextPageUrl);
    public abstract void EnrichJobDetails(IWebDriver driver, Job job);
    public abstract void Login(IWebDriver driver);

    public void ScrollTo(IWebDriver driver, IWebElement element)
    {
        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'end'})", element);
    }
    public bool CheckJobExists(string jobId, HashSet<string> jobIds)
    {
        return jobIds.Contains($"{SiteName}:{jobId}");
    }
}