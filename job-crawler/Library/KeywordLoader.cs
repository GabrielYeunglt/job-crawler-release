using System.Text.Json;
using job_crawler.Library;
using job_crawler.Models;

public static class KeywordLoader
{
    public static List<Keyword> LoadFromJson(string filePath)
    {
        var keywords = FileLibrary.LoadConfig<List<Keyword>>("Configs/keywords.json");
        foreach (var keyword in keywords)
        {
            keyword.Add(keyword.Name);
        }
        return keywords;
    }
}