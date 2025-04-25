using job_crawler.Library;
using job_crawler.Models;
using OpenQA.Selenium;

namespace job_crawler.Parsers;

public interface IJobSiteParser
{
    string StartUrl { get; init; }
    string SiteName { get; init; }
    (int, int) WaitTimeRange { get; init; }
    StaticValue.JobSites Site { get; init; }
    List<Job> ExtractJobs(IWebDriver driver, HashSet<string> jobIds);
    bool TryGetNextPageUrl(IWebDriver driver, out string nextPageUrl);
    void EnrichJobDetails(IWebDriver driver, Job job);
    void Login(IWebDriver driver);
    void ScrollTo(IWebDriver driver, IWebElement element);
    bool CheckJobExists(string jobId, HashSet<string> jobIds);
}