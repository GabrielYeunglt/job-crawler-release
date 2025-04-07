using job_crawler.Library;
using job_crawler.Models;
using job_crawler.Parsers;
using job_crawler.Services;
using job_crawler.Utils;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

public class LinkedInJobSiteParser : JobSiteParser
{
    private readonly JobSiteConfig? config;
    private readonly Credentials creds;

    public LinkedInJobSiteParser()
    {
        config = FileLibrary.LoadConfig<JobSiteConfig>("Configs/linkedin.config.json");
        StartUrl = ConfigLoader.BuildUrl(config);
        SiteName = "LinkedIn";
        WaitTimeRange = (1000, 2000);
        creds = ConfigLoader.LoadCredentials("Configs/linkedin.config.json");
    }

    public override string StartUrl { get; init; }
    public override string SiteName { get; init; }
    public override (int, int) WaitTimeRange { get; init; }

    public override void Login(IWebDriver driver)
    {
        driver.Navigate().GoToUrl("https://www.linkedin.com/login");
        WebDriverWait wait = new(driver, TimeSpan.FromSeconds(10));

        try
        {
            var emailBox = wait.Until(d => d.FindElement(By.Id("username")));
            var passBox = driver.FindElement(By.Id("password"));
            var loginButton = wait.Until(d => d.FindElement(By.CssSelector("button[type='submit']")));

            var js = (IJavaScriptExecutor)driver;
            js.ExecuteScript("document.getElementById('rememberMeOptIn-checkbox').checked = false;");
            Console.WriteLine("☑️ Unchecked 'Keep me logged in' via JavaScript");

            emailBox.SendKeys(creds.Username);
            passBox.SendKeys(EncryptionHelper.Decrypt(creds.Password));
            loginButton.Click();

            wait.Until(d => d.Url.Contains("/feed") || d.Url.Contains("/jobs"));
            Console.WriteLine("✅ Logged into LinkedIn");
        }
        catch (Exception e)
        {
            Console.WriteLine("❌ Failed to login: " + e.Message);
            throw;
        }

        Thread.Sleep(3000); // Let the page fully settle after login
    }

    public override List<Job> ExtractJobs(IWebDriver driver, HashSet<string> jobIds)
    {
        var jobs = new List<Job>();
        WebDriverWait wait = new(driver, TimeSpan.FromSeconds(10));
        Random random = new();

        // 1. Locate the scroll sentinel
        var sentinel = driver.FindElement(By.CssSelector(config.SentinelSelector));

        // 2. Get the parent container or its next sibling — safest is usually the parent container
        var jobListContainer = sentinel.FindElement(By.XPath(config.JobListSelector));

        // 3. Find all job cards within the container
        var jobCards = jobListContainer.FindElements(By.XPath(config.JobCardsSelector));

        foreach (var card in jobCards)
        {
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'end'})", card);
            Thread.Sleep(100);
        }

        foreach (var card in jobCards)
        {
            try
            {
                bool success = false;

                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    try
                    {
                        var titleLink = wait.Until(d => card.FindElement(By.CssSelector(config.JobTitleSelector)));
                        var href = titleLink.GetAttribute("href");
                        var title = titleLink.FindElement(By.CssSelector("span"))?.Text?.Trim();
                        var jobId = card.GetAttribute("data-occludable-job-id") ?? card.GetAttribute("data-job-id");
                        var company = card.FindElement(By.CssSelector(config.JobCompanySelector)).Text.Trim();

                        if (string.IsNullOrEmpty(href) || string.IsNullOrEmpty(title))
                        {
                            Console.WriteLine("⚠️ Incomplete job card (no link/title), skipping.");
                            break;
                        }

                        if (CheckJobExists(jobId, jobIds)) break;

                        card.Click();
                        Thread.Sleep(random.Next(WaitTimeRange.Item1, WaitTimeRange.Item2));

                        var locationText = wait.Until(d => d.FindElement(By.ClassName(config.JobLocationSelector)))
                            .GetAttribute("innerText");
                        var descText = wait.Until(d => d.FindElement(By.ClassName(config.JobDescriptionSelector)))
                            .GetAttribute("innerText");

                        if (href.StartsWith("/jobs/view/"))
                            href = "https://www.linkedin.com" + href;

                        var job = new Job
                        {
                            Title = title,
                            Link = href,
                            ID = jobId,
                            Site = "LinkedIn",
                            Company = company,
                            Description = descText,
                            Location = locationText
                        };
                        
                        jobs.Add(job);

                        success = true;
                        break;
                    }
                    catch (Exception retryEx)
                    {
                        Console.WriteLine($"🔁 Retry {attempt}/3 failed: {retryEx.Message}");

                        if (attempt == 3)
                        {
                            Console.WriteLine("❌ Giving up on this card after 3 attempts.");
                        }
                        else
                        {
                            Thread.Sleep(1000); // Backoff before retry
                        }
                    }
                }

                if (!success)
                {
                    Console.WriteLine("⚠️ Failed to process job card after retries.");
                }
            }
            catch (NoSuchElementException e)
            {
                Console.WriteLine("⚠️ Skipping card — " + e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine($"⚠️ Unexpected error while parsing card: {e.Message}");
            }
        }

        return jobs;
    }

    public override bool TryGetNextPageUrl(IWebDriver driver, out string nextPageUrl)
    {
        nextPageUrl = string.Empty;
        try
        {
            var nextButton = driver.FindElement(By.CssSelector(config?.NextPageButtonSelector));
            if (nextButton.Enabled)
            {
                nextButton.Click();
                return true;
            }
        }
        catch
        {
            // No next button
        }

        return false;
    }

    public override void EnrichJobDetails(IWebDriver driver, Job job)
    {
        // logic moved to extractJobs()
    }
}