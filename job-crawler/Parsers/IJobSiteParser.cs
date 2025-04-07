using job_crawler.Models;
using OpenQA.Selenium;

namespace job_crawler.Parsers;

public interface IJobSiteParser
{
    string StartUrl { get; init; }
    string SiteName { get; init; }
    (int, int) WaitTimeRange { get; init; }
    List<Job> ExtractJobs(IWebDriver driver, HashSet<string> jobIds);
    bool TryGetNextPageUrl(IWebDriver driver, out string nextPageUrl);
    void EnrichJobDetails(IWebDriver driver, Job job);
    void Login(IWebDriver driver);

    bool CheckJobExists(string jobId, HashSet<string> jobIds);
}