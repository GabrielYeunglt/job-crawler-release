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

        var parsers = new List<IJobSiteParser>
        {
            new IndeedJobSiteParser(),
            new LinkedInJobSiteParser()
            // Add more sites here easily
        };

        foreach (var parser in parsers)
        {
            try
            {
                Console.WriteLine($"{parser.GetType().Name} starts crawling.");
                var siteJobs = Crawl(parser, oldRecords);
                if (siteJobs.Any())
                    jobs.AddRange(siteJobs);
                Console.WriteLine($"{parser.GetType().Name} done crawling.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"{parser.GetType().Name} encountered a problem:");
                Console.WriteLine(e);
            }
        }

        jobs.Sort((a, b) => b.Score.CompareTo(a.Score));

        try
        {
            Console.WriteLine("Consolidate read jobs.");
            FileLibrary.SaveHandler.SaveJobIndexLine(jobs);
        }
        catch (Exception e)
        {
            Console.WriteLine("Saving historical record encountered a problem:");
            Console.WriteLine(e);
        }

        try
        {
            Console.WriteLine($"{jobs.Count} job(s) found. Output file.");
            FileLibrary.SaveHandler.SaveJobsToCsv(jobs, filepath);
        }
        catch (Exception e)
        {
            Console.WriteLine("Error saving to CSV:");
            Console.WriteLine(e);
            throw;
        }
    }
}