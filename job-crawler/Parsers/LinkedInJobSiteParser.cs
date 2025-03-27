using job_crawler.Library;
using job_crawler.Models;
using job_crawler.Parsers;
using job_crawler.Utils;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

public class LinkedInJobSiteParser : IJobSiteParser
{
    private readonly JobSiteConfig? config;
    private readonly Credentials creds;

    public LinkedInJobSiteParser()
    {
        config = FileLibrary.LoadConfig<JobSiteConfig>("Configs/linkedin.config.json");
        StartUrl = ConfigLoader.BuildUrl(config);
        creds = ConfigLoader.LoadCredentials("Configs/linkedin.config.json");
    }

    public void Login(IWebDriver driver)
    {
        driver.Navigate().GoToUrl("https://www.linkedin.com/login");
        WebDriverWait wait = new(driver, TimeSpan.FromSeconds(10));

        try
        {
            var emailBox = wait.Until(d => d.FindElement(By.Id("username")));
            var passBox = driver.FindElement(By.Id("password"));
            var loginButton = driver.FindElement(By.CssSelector("button[type='submit']"));

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


    public string StartUrl { get; }

    public List<Job> ExtractJobs(IWebDriver driver)
    {
        var jobs = new List<Job>();

        // var jobCards = driver.FindElements(By.CssSelector(config.JobListSelector));

        // 1. Locate the scroll sentinel
        var sentinel = driver.FindElement(By.CssSelector(config.SentinelSelector));

        // 2. Get the parent container or its next sibling — safest is usually the parent container
        var jobListContainer = sentinel.FindElement(By.XPath(config.JobListSelector));

        // 3. Find all job cards within the container
        var jobCards = jobListContainer.FindElements(By.XPath(config.JobCardsSelector));


        foreach (var card in jobCards)
            try
            {
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'end'})", card);
                Thread.Sleep(100);

                // var titleLink = card.FindElement(By.CssSelector("a[class*='job-card-list__title--link']"));
                var titleLink = card.FindElement(By.CssSelector(config.JobTitleSelector));

                // Skip cards where link or title is missing
                var href = titleLink.GetAttribute("href");
                // ✅ Get only the first visible title span (skip visually-hidden)
                var titleSpan = titleLink.FindElement(By.CssSelector("span"));
                var title = titleSpan.Text?.Trim();
                var jobId = card.GetAttribute("data-occludable-job-id") ??
                            card.GetAttribute("data-job-id");
                var companySpan = card.FindElement(By.CssSelector(config.JobCompanySelector));
                string company = companySpan.Text.Trim();


                if (string.IsNullOrEmpty(href) || string.IsNullOrEmpty(title))
                {
                    Console.WriteLine("⚠️ Incomplete job card (no link/title), skipping.");
                    continue;
                }

                // Make sure we use full LinkedIn link
                if (href.StartsWith("/jobs/view/")) href = "https://www.linkedin.com" + href;

                jobs.Add(new Job
                {
                    Title = title,
                    Link = href,
                    ID = jobId,
                    Site = "LinkedIn",
                    Company = company
                });
            }
            catch (NoSuchElementException)
            {
                Console.WriteLine("⚠️ Skipping card — no job title link found.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"⚠️ Unexpected error while parsing card: {e.Message}");
            }


        return jobs;
    }

    public bool TryGetNextPageUrl(IWebDriver driver, out string nextPageUrl)
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

    public void EnrichJobDetails(IWebDriver driver, Job job)
    {
        driver.Navigate().GoToUrl(job.Link);
        WebDriverWait wait = new(driver, TimeSpan.FromSeconds(10));

        try
        {
            var desc = wait.Until(d =>
                d.FindElement(By.CssSelector(config.JobDescriptionSelector)));
            job.Description = desc.Text;

            var location = driver.FindElements(By.CssSelector(config.JobLocationSelector)).FirstOrDefault()?.Text;
            job.Location = location?.Trim();
        }
        catch (Exception e)
        {
            job.Error = e.Message;
        }

        Thread.Sleep(new Random().Next(500, 1000));
    }
}