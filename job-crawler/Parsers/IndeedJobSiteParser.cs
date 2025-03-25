using job_crawler.Library;
using job_crawler.Models;
using job_crawler.Services;
using job_crawler.Utils;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace job_crawler.Parsers;

public class IndeedJobSiteParser : IJobSiteParser
{
    private readonly JobAnalyzeService analyzeService;
    private readonly JobSiteConfig? config;

    public IndeedJobSiteParser()
    {
        config = FileLibrary.LoadConfig<JobSiteConfig>("Configs/indeed.config.json");
        StartUrl = ConfigLoader.BuildUrl(config);
        analyzeService = new JobAnalyzeService();
    }

    public void Login(IWebDriver driver)
    {
        // No need to login
    }

    public string StartUrl { get; }

    // public List<Job> ExtractJobs(IWebDriver driver)
    // {
    //     var jobs = new List<Job>();
    //     var jobElements = driver.FindElements(By.XPath(config.JobTitleSelector));
    //
    //     foreach (var job in jobElements)
    //     {
    //         var link = job.GetAttribute("href");
    //         jobs.Add(new Job
    //         {
    //             Title = job.Text,
    //             Link = link,
    //             ID = analyzeService.GetJobIdFromUrl(link),
    //             Site = "Indeed"
    //         });
    //     }
    //
    //     return jobs;
    // }
    public List<Job> ExtractJobs(IWebDriver driver)
    {
        var jobs = new List<Job>();
        var jobElements = driver.FindElements(By.XPath(config.JobTitleSelector));

        foreach (var job in jobElements)
        {
            var link = job.GetAttribute("href");

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
            catch (NoSuchElementException) { }

            // 📍 Extract location
            string location = "";
            try
            {
                var locationElement = jobCard.FindElement(By.CssSelector("div[data-testid='text-location']"));
                location = locationElement.Text.Trim();
            }
            catch (NoSuchElementException) { }

            jobs.Add(new Job
            {
                Title = job.Text,
                Link = link,
                ID = analyzeService.GetJobIdFromUrl(link),
                Site = "Indeed",
                Company = company,
                Location = location
            });
        }

        return jobs;
    }


    public bool TryGetNextPageUrl(IWebDriver driver, out string nextPageUrl)
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

    public void EnrichJobDetails(IWebDriver driver, Job job)
    {
        driver.Navigate().GoToUrl(job.Link);
        WebDriverWait wait = new(driver, TimeSpan.FromSeconds(10));

        try
        {
            var desc = wait.Until(d => d.FindElements(By.Id(config.JobDescriptionSelector)));

            job.Description = desc.FirstOrDefault()?.Text;
        }
        catch (Exception e)
        {
            job.Error = e.Message;
        }

        Thread.Sleep(new Random().Next(500, 3000));
    }
}