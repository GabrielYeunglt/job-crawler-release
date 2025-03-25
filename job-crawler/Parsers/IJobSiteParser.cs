using job_crawler.Models;
using OpenQA.Selenium;

namespace job_crawler.Parsers;

public interface IJobSiteParser
{
    string StartUrl { get; }
    List<Job> ExtractJobs(IWebDriver driver);
    bool TryGetNextPageUrl(IWebDriver driver, out string nextPageUrl);
    void EnrichJobDetails(IWebDriver driver, Job job);
    void Login(IWebDriver driver);
}