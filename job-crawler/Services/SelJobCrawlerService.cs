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
        ((IJavaScriptExecutor)driver).ExecuteScript("Object.defineProperty(navigator, 'webdriver', {get: () => undefined})");
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
            jobs.AddRange(parser.ExtractJobs(driver, existingJobs));
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
        
        foreach (var job in jobs)
        {
            analyzeService.AnalyzeJob(job);
        }

        jobs.Sort((a, b) => b.Score.CompareTo(a.Score));
        return jobs;
    }

    public void Crawl(string filepath = "crawled", StaticValue.JobSites jobSites = StaticValue.JobSites.All)
    {
        var (prevDayRec, samedayRec) = FileLibrary.SaveHandler.LoadJobIndexRecords();
        var oldRecords = prevDayRec.Union(samedayRec).ToHashSet();
        var jobs = new List<Job>();

        var allParsers = new List<IJobSiteParser>
        {
            new IndeedJobSiteParser(),
            new LinkedInJobSiteParser(),
            new GlassdoorJobSiteParser()
        };

        var parsers = jobSites switch
        {
            StaticValue.JobSites.All => allParsers,
            _ => allParsers.Where(p => p.Site == jobSites).ToList()
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

        // Deduplicate
        jobs = jobs.Distinct().ToList();
        // Sort
        jobs.Sort((a, b) => b.Score.CompareTo(a.Score));

        try
        {
            Console.WriteLine("Consolidate read jobs.");
            FileLibrary.SaveHandler.SaveJobIndexLine(jobs, samedayRec);
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

    public void CrawlSingleJob(string url)
    {
        if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
        StaticValue.JobSites site = JobAnalyzeService.GetJobSiteTypeFromUrl(url);

        IJobSiteParser parser = site switch
        {
            StaticValue.JobSites.LinkedIn => new LinkedInJobSiteParser(),
            StaticValue.JobSites.Indeed => new IndeedJobSiteParser(),
            _ => throw new NotSupportedException()
        };

        parser.Login(driver);

        var analyzeService = new JobAnalyzeService();

        Job job = new();
        job.ID = JobAnalyzeService.GetJobIdFromUrl(url);
        job.Link = url;
        job.Site = site switch
        {
            StaticValue.JobSites.Indeed => "Indeed",
            StaticValue.JobSites.LinkedIn => "LinkedIn",
        };

        parser.EnrichJobDetails(driver, job);
        analyzeService.AnalyzeJob(job);

        Console.WriteLine($"Analyzing finished, score: {job.Score}");
    }
}