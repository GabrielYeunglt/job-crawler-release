namespace job_crawler.Models;

public class JobSiteConfig
{
    public string BaseUrl { get; set; }
    public Dictionary<string, string> QueryParams { get; set; }
    public string JobTitleSelector { get; set; }
    public string JobCompanySelector { get; set; }
    public string NextPageButtonSelector { get; set; }
    public string JobDescriptionSelector { get; set; }

    public string JobLocationSelector { get; set; }

    // Linkedin
    public string SentinelSelector { get; set; }
    public string JobListSelector { get; set; }
    public string JobCardsSelector { get; set; }
}