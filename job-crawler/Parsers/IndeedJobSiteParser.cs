using job_crawler.Library;
using job_crawler.Models;
using job_crawler.Services;
using job_crawler.Utils;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace job_crawler.Parsers;

public class IndeedJobSiteParser : JobSiteParser
{
    private readonly JobSiteConfig? config;

    public IndeedJobSiteParser()
    {
        config = FileLibrary.LoadConfig<JobSiteConfig>("Configs/indeed.config.json");
        StartUrl = ConfigLoader.BuildUrl(config);
        SiteName = "Indeed";
        WaitTimeRange = (500, 3000);
        Site = StaticValue.JobSites.Indeed;
    }

    public override string StartUrl { get; init; }
    public override string SiteName { get; init; }
    public override (int, int) WaitTimeRange { get; init; }
    public override StaticValue.JobSites Site { get; init; }

    public override void Login(IWebDriver driver)
    {
        // No need to login, but let it load a while
        Thread.Sleep(new Random().Next(WaitTimeRange.Item1, WaitTimeRange.Item2));
    }

    public override List<Job> ExtractJobs(IWebDriver driver, HashSet<string> jobIds)
    {
        var jobs = new List<Job>();
        var random = new Random();
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(random.Next(10)));
        var jobCards = driver.FindElements(By.XPath(config.JobTitleSelector));

        foreach (var card in jobCards)
        {
            ScrollTo(driver, card);
            Thread.Sleep(100);
        }

        foreach (var job in jobCards)
        {
            var link = job.GetAttribute("href");
            var jobId = JobAnalyzeService.GetJobIdFromUrl(link);

            if (CheckJobExists(jobId, jobIds))
                continue;

            // 🔍 Find parent job card to look for company and location
            IWebElement jobCard = null;
            try
            {
                jobCard = job.FindElement(By.XPath("./ancestor::li[contains(@class, 'css-1ac2h1w')]"));
            }
            catch (NoSuchElementException)
            {
                // Fallback: try higher up
                jobCard = job.FindElement(By.XPath("./ancestor::div[contains(@class, 'job_seen_beacon')]"));
            }

            // 🏢 Extract company name
            string company = "";
            try
            {
                var companyElement = jobCard.FindElement(By.CssSelector("span[data-testid='company-name']"));
                company = companyElement.Text.Trim();
            }
            catch (NoSuchElementException)
            {
            }

            // 📍 Extract location
            string location = "";
            try
            {
                var locationElement = jobCard.FindElement(By.CssSelector("div[data-testid='text-location']"));
                location = locationElement.Text.Trim();
            }
            catch (NoSuchElementException)
            {
            }

            job.Click();
            Thread.Sleep(random.Next(WaitTimeRange.Item1, WaitTimeRange.Item2));

            var card = wait.Until(d => d.FindElement(By.ClassName("jobsearch-JobComponent-description")));
            ScrollTo(driver, card);
            
            var descText = wait.Until(d => card.FindElement(By.Id(config.JobDescriptionSelector)))
                .GetAttribute("innerText");
            
            jobs.Add(new Job
            {
                Title = job.Text,
                Link = link,
                ID = jobId,
                Site = "Indeed",
                Company = company,
                Location = location,
                Description = descText,
            });
        }

        return jobs;
    }


    public override bool TryGetNextPageUrl(IWebDriver driver, out string nextPageUrl)
    {
        nextPageUrl = string.Empty;
        try
        {
            var nextButton = driver.FindElement(By.CssSelector(config.NextPageButtonSelector));
            if (nextButton != null && nextButton.Displayed)
            {
                nextPageUrl = nextButton.GetAttribute("href");
                return true;
            }
        }
        catch (NoSuchElementException)
        {
            Console.WriteLine("No more pages.");
        }

        return false;
    }

    public override void EnrichJobDetails(IWebDriver driver, Job job)
    {
        // logic moved to extractJobs()
    }
}