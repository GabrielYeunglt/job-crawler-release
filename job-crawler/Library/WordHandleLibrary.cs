using job_crawler.Models;

namespace job_crawler.Library;

public static class WordHandleLibrary
{
    public static Dictionary<string, int> BreakdownWords(string paragraph)
    {
        if (string.IsNullOrEmpty(paragraph)) return new Dictionary<string, int>();

        // Define delimiters (space, punctuation, etc.)
        var delimiters = new[] { ' ', ',', '.', '!', '?', ';', ':', '\n', '\r', '\t' };

        // Split string into words
        var words = paragraph.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

        Dictionary<string, int> breakdownWords = new Dictionary<string, int>();

        foreach (var word in words)
            if (breakdownWords.ContainsKey(word))
                breakdownWords[word]++;
            else
                breakdownWords.Add(word, 1);

        return breakdownWords;
    }

    public static double AnalyzeWord(List<Keyword> keywords, Dictionary<string, int> breakdownWords)
    {
        double score = 0;
        int totalWeight = keywords.Sum(k => Math.Abs(k.Weight)); // Total weight range (for normalization)

        foreach (var keyword in keywords)
        {
            if (breakdownWords.Keys.Any(word => keyword.Compare(word)))
            {
                score += keyword.Weight;
            }
        }

        // Normalize to percentage scale (-100 to +100)
        return totalWeight > 0 ? Math.Round(score / totalWeight * 100, 2) : 0;
    }
}