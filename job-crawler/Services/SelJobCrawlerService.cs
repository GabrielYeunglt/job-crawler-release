using job_crawler.Library;
using job_crawler.Models;
using job_crawler.Parsers;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace job_crawler.Services;

public class SelJobCrawlerService : IDisposable
{
    private static readonly Random random = new();
    private readonly bool debug = true;
    private bool disposed;
    private readonly IWebDriver driver;

    public SelJobCrawlerService()
    {
        var options = new ChromeOptions();
        options.AddArgument("--disable-blink-features=AutomationControlled");
        options.AddArgument("--remote-allow-origins=*");
        driver = new ChromeDriver(options);
        Console.WriteLine("WebDriver started.");
    }

    public void Dispose()
    {
        if (!disposed)
        {
            driver.Quit();
            Console.WriteLine("WebDriver closed.");
            disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    public List<Job> Crawl(IJobSiteParser parser, HashSet<string> existingJobs)
    {
        parser.Login(driver);

        var analyzeService = new JobAnalyzeService();
        var jobs = new List<Job>();

        driver.Navigate().GoToUrl(parser.StartUrl);
        Thread.Sleep(random.Next(3000, 7000));

        bool hasMorePages;
        do
        {
            var nextUrl = string.Empty;
            jobs.AddRange(parser.ExtractJobs(driver));
            hasMorePages = !debug && parser.TryGetNextPageUrl(driver, out nextUrl);

            if (hasMorePages)
            {
                if (!string.IsNullOrWhiteSpace(nextUrl))
                {
                    // LinkedIn doesn't use a for next page
                    driver.Navigate().GoToUrl(nextUrl);
                }

                Thread.Sleep(random.Next(3000, 7000));
            }
        } while (hasMorePages);

        // Deduplicate
        var deduplicatedJobs = jobs
            .Where(job => !existingJobs.Contains($"{job.Site}:{job.ID}"))
            .ToList();


        foreach (var job in deduplicatedJobs)
        {
            parser.EnrichJobDetails(driver, job);
            analyzeService.AnalyzeJob(job);
            if (debug)
            {
                break;
            }

            Thread.Sleep(random.Next(1000, 5000));
        }

        deduplicatedJobs.Sort((a, b) => b.Score.CompareTo(a.Score));
        return deduplicatedJobs;
    }

    public void Crawl(string filepath = "crawled")
    {
        var oldRecords = FileLibrary.SaveHandler.LoadJobIndexLine();
        var jobs = new List<Job>();
        var indeedJobs = Crawl(new IndeedJobSiteParser(), oldRecords);
        if (indeedJobs.Any()) jobs.AddRange(indeedJobs);
        var linkedinJobs = Crawl(new LinkedInJobSiteParser(), oldRecords);
        if (linkedinJobs.Any()) jobs.AddRange(linkedinJobs);
        jobs.Sort((a, b) => b.Score.CompareTo(a.Score));
        FileLibrary.SaveHandler.SaveJobIndexLine(jobs);
        FileLibrary.SaveHandler.SaveJobsToCsv(jobs, filepath);
    }
}