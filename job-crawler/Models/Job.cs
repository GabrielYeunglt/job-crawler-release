namespace job_crawler.Models;

public class Job
{
    public string ID { get; set; }
    public string Company { get; set; }
    public string Site { get; set; }
    public string Title { get; set; }
    public string Link { get; set; }
    public string Description { get; set; }
    public string Location { get; set; }
    public double Score { get; set; }
    public string Error { get; set; }
    public override bool Equals(object? obj)
    {
        if (obj is not Job other) return false;
        return string.Equals(ID, other.ID, StringComparison.OrdinalIgnoreCase)
               && string.Equals(Site, other.Site, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ID?.ToLowerInvariant(), Site?.ToLowerInvariant());
    }
}