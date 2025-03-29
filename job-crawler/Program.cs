using job_crawler.Library;
using job_crawler.Services;

Console.WriteLine("🔍 Job Crawler Tool");
Console.WriteLine("====================");
Console.WriteLine("1: Run job crawler");
Console.WriteLine("2: Consolidate save");
Console.Write("Choose an option (1 or 2): ");

string? action;
do
{
    action = Console.ReadLine()?.Trim();
} while (action != "1" && action != "2");

try
{
    switch (action)
    {
        case "1":
            var path = FileLibrary.AskForFilePath();
            using (var crawler = new SelJobCrawlerService())
            {
                crawler.Crawl(path);
            }
            break;

        case "2":
            FileLibrary.SaveHandler.SaveOldRecToSave();
            Console.WriteLine("✅ Consolidated save completed.");
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