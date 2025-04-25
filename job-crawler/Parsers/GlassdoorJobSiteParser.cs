using job_crawler.Library;
using job_crawler.Models;
using job_crawler.Parsers;
using job_crawler.Services;
using job_crawler.Utils;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

public class GlassdoorJobSiteParser : JobSiteParser
{
    private readonly JobSiteConfig? config;
    private readonly Credentials creds;

    public GlassdoorJobSiteParser()
    {
        config = FileLibrary.LoadConfig<JobSiteConfig>("Configs/glassdoor.config.json");
        StartUrl = ConfigLoader.BuildUrl(config);
        SiteName = "Glassdoor";
        WaitTimeRange = (1000, 2000);
        creds = ConfigLoader.LoadCredentials("Configs/glassdoor.config.json");
        Site = StaticValue.JobSites.Glassdoor;
    }

    public override string StartUrl { get; init; }
    public override string SiteName { get; init; }
    public override (int, int) WaitTimeRange { get; init; }
    public override StaticValue.JobSites Site { get; init; }

    public override void Login(IWebDriver driver)
    {
        driver.Navigate().GoToUrl(config.LoginUrl);
        WebDriverWait wait = new(driver, TimeSpan.FromSeconds(10));

        try
        {
            var emailBox = wait.Until(d => d.FindElement(By.Id("inlineUserEmail")));
            emailBox.SendKeys(creds.Username);
            var loginButton = wait.Until(d => d.FindElement(By.CssSelector("button[type='submit']")));
            loginButton.Click();

            Thread.Sleep(new Random().Next(WaitTimeRange.Item1, WaitTimeRange.Item2));

            var passBox = wait.Until(d => d.FindElement(By.Id("inlineUserPassword")));
            passBox.SendKeys(EncryptionHelper.Decrypt(creds.Password));
            // need to find again as it is different button
            loginButton = wait.Until(d => d.FindElement(By.CssSelector("button[type='submit']")));
            loginButton.Click();

            wait.Until(d => d.Url.Contains("/Community") || d.Url.Contains("/jobs"));
            Console.WriteLine("✅ Logged into Glassdoor");
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

        {
            // close dialog box
            var dialog = wait.Until(d => d.FindElement(By.XPath("//div[contains(@role, 'dialog')]")));
            // <div role="dialog" aria-live="polite" aria-modal="true" class="modal_Modal__wyPlr" data-size-variant="full" style="--modal-max-width: 430px;">
            var closeDialogButton = dialog.FindElement(By.XPath(".//button[@aria-label='Cancel']"));
            closeDialogButton.Click();

            // click "Show more jobs" button until there is no more
            try
            {
                while (true)
                {
                    var showMoreJobButton =  wait.Until(d => d.FindElement(By.XPath(config.NextPageButtonSelector)));

                    if (showMoreJobButton == null || !showMoreJobButton.Displayed)
                    {
                        break; // Button is gone or not visible → we're done
                    }

                    ScrollTo(driver, showMoreJobButton);
                    showMoreJobButton.Click();

                    Thread.Sleep(random.Next(WaitTimeRange.Item1, WaitTimeRange.Item2));
                }
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine("✅ No more 'Show more jobs' button found.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"⚠️ Unexpected error while expanding job list: {e.Message}");
            }
        }

        // 1. Locate the scroll sentinel
        var sentinel = driver.FindElement(By.XPath(config.SentinelSelector));

        // 2. Find all job cards within the container
        var jobCards = sentinel.FindElements(By.XPath(config.JobCardsSelector));

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
                        var titleLink = wait.Until(d => card.FindElement(By.XPath(config.JobTitleSelector)));
                        var title = titleLink.Text?.Trim();
                        var href = titleLink.GetAttribute("href");
                        var jobId = card.GetAttribute("data-jobid") ?? card.GetAttribute("data-job-id");
                        var company = card.FindElement(By.XPath(config.JobCompanySelector)).Text.Trim();
                        var location = card.FindElement(By.XPath(config.JobLocationSelector)).Text.Trim();
        
                        if (string.IsNullOrEmpty(href) || string.IsNullOrEmpty(title))
                        {
                            Console.WriteLine("⚠️ Incomplete job card (no link/title), skipping.");
                            break;
                        }
        
                        if (CheckJobExists(jobId, jobIds)) break;
        
                        card.Click();
                        Thread.Sleep(random.Next(WaitTimeRange.Item1, WaitTimeRange.Item2));
                        
                        var descText = wait.Until(d => d.FindElement(By.XPath(config.JobDescriptionSelector)))
                            .GetAttribute("innerText");
        
                        if (href.StartsWith("/job-listing"))
                            href = "https://www.glassdoor.ca" + href;
        
                        var job = new Job
                        {
                            Title = title,
                            Link = href,
                            ID = jobId,
                            Site = "Glassdoor",
                            Company = company,
                            Description = descText,
                            Location = location,
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
            var nextButton = driver.FindElement(By.XPath(config?.NextPageButtonSelector));
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