using job_crawler.Library;
using job_crawler.Models;
using job_crawler.Parsers;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace job_crawler.Services;

public class SelJobCrawlerService : IDisposable
{
    private static readonly Random random = new();
    private readonly bool debug = false;
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

    public List<Job> Crawl(IJobSiteParser parser, List<Job> existingJobs)
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
        jobs = jobs.Where(job => !existingJobs.Contains(job)).GroupBy(job => job.ID).Select(group => group.First()).ToList();

        foreach (var job in jobs)
        {
            parser.EnrichJobDetails(driver, job);
            analyzeService.AnalyzeJob(job);
            if (debug)
            {
                break;
            }
            Thread.Sleep(random.Next(1000, 5000));
        }

        jobs.Sort((a, b) => b.Score.CompareTo(a.Score));
        return jobs;
    }

    public void Crawl(string filepath = "crawled")
    {
        // Read old records so that new one don't have to be processed again
        List<Job> old_records = new();
        var existingFiles = FileLibrary.GetCsvFilesFromDirectory(FileLibrary.DEFAULT_PATH);
        foreach (var file in existingFiles)
        {
            var records = FileLibrary.ReadJobsFromCsv(file);
            if (records != null)
            {
                old_records.AddRange(records);
            }
        }
        old_records = old_records.Distinct().ToList();
        
        var jobs = new List<Job>();
        var indeedJobs = Crawl(new IndeedJobSiteParser(), old_records);
        if (indeedJobs.Any()) jobs.AddRange(indeedJobs);
        var linkedinJobs = Crawl(new LinkedInJobSiteParser(), old_records);
        if (linkedinJobs.Any()) jobs.AddRange(linkedinJobs);
        jobs.Sort((a, b) => b.Score.CompareTo(a.Score));
        SaveJobsToCSV(jobs, filepath);
    }

    private static void SaveJobsToCSV(List<Job> jobs, string filename)
    {
        using var writer = new StreamWriter(filename);
        writer.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
        writer.WriteLine("Title,Company,Site,Location,Score,Link,ID");

        foreach (var job in jobs)
            writer.WriteLine($"\"{job.Title}\",\"{job.Company}\",\"{job.Site}\",\"{job.Location}\",\"{job.Score}\",\"{job.Link}\",\"{job.ID}\"");
    }
}