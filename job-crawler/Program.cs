// See https://aka.ms/new-console-template for more information

using job_crawler.Library;
using job_crawler.Services;

Console.WriteLine("Hello, World!");

var path = FileLibrary.AskForFilePath();

var crawler = new SelJobCrawlerService();
crawler.Crawl(path);
crawler.Dispose();