using job_crawler.Library;
using job_crawler.Services;
using OpenQA.Selenium.DevTools.V131.Console;

Console.WriteLine("🔍 Job Crawler Tool");
Console.WriteLine("====================");
Console.WriteLine("0: Encryption helper");
Console.WriteLine("1: Run job crawler");
Console.WriteLine("2: Consolidate save");
Console.WriteLine("3: Analyze score for provided job");
Console.Write("Choose an option: ");

string? action;
int actionIndex;
do
{
    action = Console.ReadLine()?.Trim();
} while (!int.TryParse(action, out actionIndex));


try
{
    switch (actionIndex)
    {
        case 0:
            string? input = string.Empty;
            do
            {
                Console.Write("Enter string to encrypt: ");
                input = Console.ReadLine();
            } while (string.IsNullOrEmpty(input));

            Console.WriteLine("Result:");
            Console.WriteLine(EncryptionHelper.Encrypt(input));
            break;
        case 1:
            Console.Write("Please choose the site to crawl:");
            Console.WriteLine("====================");
            Console.WriteLine("1: All");
            Console.WriteLine("2: Indeed");
            Console.WriteLine("3: LinkedIn");
            Console.WriteLine("4: Glassdoor");
            Console.Write("Choose an option: ");
            string? site;
            int siteIndex;
            do
            {
                site = Console.ReadLine()?.Trim();
            } while (!int.TryParse(site, out siteIndex) && Enum.IsDefined(typeof(StaticValue.JobSites), siteIndex));

            var path = FileLibrary.AskForFilePath();
            using (var crawler = new SelJobCrawlerService())
            {
                crawler.Crawl(path, (StaticValue.JobSites)(siteIndex - 1));
            }

            break;

        case 2:
            FileLibrary.SaveHandler.SaveOldRecToSave();
            Console.WriteLine("✅ Consolidated save completed.");
            break;

        case 3:
            Console.Write("Enter the full job post url for analyze: ");
            var url = Console.ReadLine()?.Trim() ?? "";
            using (var crawler = new SelJobCrawlerService())
            {
                crawler.CrawlSingleJob(url);
            }

            break;
    }
}
catch (Exception ex)
{
    Console.WriteLine("❌ An unexpected error occurred:");
    Console.WriteLine(ex);
}

Console.WriteLine("Press any key to exit...");
Console.ReadKey();