namespace job_crawler.Models;

public class Keyword
{
    public enum KeywordType
    {
        Title = 0,
        Description = 1
    }

    public Keyword(string name, int weight = 1, KeywordType type = KeywordType.Description)
    {
        Name = name;
        Weight = weight;
        Type = type;
        Synonyms = new List<string> { Normalize(name) };
    }

    public string Name { get; set; }

    public List<string> Synonyms { get; set; }
    public int Weight { get; set; } = 1;
    public KeywordType Type { get; set; } = KeywordType.Description; // default

    public void Add(string synonym)
    {
        if (!string.IsNullOrWhiteSpace(synonym))
            Synonyms.Add(Normalize(synonym));
    }

    public void AddSynonyms(IEnumerable<string> synonyms)
    {
        foreach (var synonym in synonyms) Add(synonym);
    }

    public bool Compare(string keyword)
    {
        return Synonyms.Any(synonym => string.Equals(Normalize(keyword), synonym, StringComparison.OrdinalIgnoreCase));
    }

    private string Normalize(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return keyword;
        return keyword.Replace(" ", "").ToLower();
    }
}