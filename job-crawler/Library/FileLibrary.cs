using System.Text.Json;
using job_crawler.Models;

namespace job_crawler.Library;

public class FileLibrary
{
    public readonly static string DEFAULT_PATH = "~/Documents/jobs/";
    public readonly static string DEFAULT_FILENAME = "crawled-job";

    public static string AskForFilePath()
    {
        var isValidPath = false;
        var folder = "";
        var fileName = "";

        while (!isValidPath)
        {
            Console.Write("Enter the full path to save the file (with or without filename): ");
            var fullPath = Console.ReadLine()?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(fullPath))
            {
                Console.WriteLine("Default path would be used. ~/Documents/jobs/");
                fullPath = DEFAULT_PATH;
            }

            isValidPath = RetrieveFolderAndFileName(fullPath, out folder, out fileName);
        }

        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var finalFileName = $"{fileName}-{timestamp}.csv";
        return Path.Combine(folder, finalFileName);
    }

    private static bool CheckAndCreateDirectory(string path)
    {
        bool exists = Directory.Exists(path);
        if (exists) return exists;
        Console.WriteLine("⚠️ Folder does not exist. Creating it...");
        Directory.CreateDirectory(path);
        return exists;
    }

    private static bool RetrieveFolderAndFileName(string path, out string folder, out string fileName)
    {
        bool isValidPath = false;
        folder = string.Empty;
        fileName = string.Empty;

        // Expand ~ to home directory if needed
        string fullPath = ExpandHome(path);

        try
        {
            fullPath = Path.GetFullPath(fullPath);

            // If it's an existing folder or ends with a slash, use default file name
            if (Directory.Exists(fullPath) || fullPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folder = fullPath;
                fileName = DEFAULT_FILENAME;
            }
            else
            {
                folder = Path.GetDirectoryName(fullPath) ?? "";
                fileName = Path.GetFileName(fullPath);
            }

            CheckAndCreateDirectory(folder);

            isValidPath = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Invalid path: {ex.Message}");
            isValidPath = false;
        }

        return isValidPath;
    }

    private static string ExpandHome(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return path;

        if (path.StartsWith("~"))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home,
                path.Substring(1).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }

        return path;
    }

    public static T? LoadConfig<T>(string path)
    {
        var json = File.ReadAllText(GetRelativePath(path));

        JsonSerializerOptions? options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        return JsonSerializer.Deserialize<T>(json, options);
    }

    private static string GetRelativePath(string path)
    {
        return Path.Combine(AppContext.BaseDirectory, path);
    }

    public static List<string> GetCsvFilesFromDirectory(string folderPath)
    {
        var csvFiles = new List<string>();
        folderPath = ExpandHome(folderPath);

        if (!Directory.Exists(folderPath))
        {
            Console.WriteLine($"❌ Directory not found: {folderPath}");
            return csvFiles;
        }

        try
        {
            var files = Directory.GetFiles(folderPath, "*.csv", SearchOption.TopDirectoryOnly);
            csvFiles.AddRange(files);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error retrieving CSV files: {ex.Message}");
        }

        return csvFiles;
    }

    public static List<Job> ReadJobsFromCsv(string path)
    {
        var jobs = new List<Job>();

        if (!File.Exists(path))
        {
            Console.WriteLine($"❌ File not found: {path}");
            return jobs;
        }

        try
        {
            using var reader = new StreamReader(path);
            reader.ReadLine(); // skip timestamp line
            string? headerLine = reader.ReadLine(); // read actual CSV header
            if (headerLine == null) return jobs;

            var headers = ParseCsvLine(headerLine);
            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < headers.Count; i++)
            {
                headerMap[headers[i]] = i;
            }

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var fields = ParseCsvLine(line);
                if (fields.Count < headers.Count) continue; // skip malformed lines

                jobs.Add(new Job
                {
                    Title = GetField(fields, headerMap, "Title"),
                    Company = GetField(fields, headerMap, "Company"),
                    Site = GetField(fields, headerMap, "Site"),
                    Location = GetField(fields, headerMap, "Location"),
                    Link = GetField(fields, headerMap, "Link"),
                    ID = GetField(fields, headerMap, "ID"),
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error reading jobs from CSV: {ex.Message}");
        }

        return jobs;
    }

    // Helper to get a value by column name
    private static string GetField(List<string> fields, Dictionary<string, int> map, string key)
    {
        return map.TryGetValue(key, out int index) && index < fields.Count
            ? fields[index].Trim()
            : "";
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = "";
        bool inQuotes = false;

        foreach (char c in line)
        {
            switch (c)
            {
                case '\"':
                    inQuotes = !inQuotes;
                    break;
                case ',' when !inQuotes:
                    fields.Add(current.Trim());
                    current = "";
                    break;
                default:
                    current += c;
                    break;
            }
        }

        fields.Add(current.Trim()); // add last field
        return fields;
    }

    public static class SaveHandler
    {
        public const string DefaultPath = "history/save";

        public static void SaveJobIndexLine(List<Job> jobs, string path = DefaultPath)
        {
            RetrieveFolderAndFileName(path, out var folder, out var fileName);
            string fullpath = Path.Combine(folder, $"{fileName}-{DateTime.Now:yyyyMMdd}.csv");

            // ✅ Load existing keys from index file
            var combinedKeys = LoadJobIndexLine(fullpath);

            // ✅ Add new keys from current job list
            foreach (var job in jobs)
            {
                combinedKeys.Add($"{job.Site}:{job.ID}");
            }

            // ✅ Ensure folder exists before writing
            Directory.CreateDirectory(folder);

            using var writer = new StreamWriter(fullpath);
            writer.WriteLine(string.Join("|", combinedKeys));
        }

        public static HashSet<string> LoadJobIndexLine(string path = DefaultPath)
        {
            if (string.Equals(path, DefaultPath, StringComparison.OrdinalIgnoreCase))
            {
                path = $"{DefaultPath}-{DateTime.Now:yyyyMMdd}.csv";
            }

            var set = new HashSet<string>();

            if (!File.Exists(path)) return set;

            var content = File.ReadAllText(path);
            var parts = content.Split('|', StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
                set.Add(part.Trim());

            return set;
        }

        public static void SaveJobsToCsv(List<Job> jobs, string filename)
        {
            if (string.IsNullOrEmpty(filename)) filename = DEFAULT_FILENAME;
            using var writer = new StreamWriter(filename);
            writer.WriteLine($"{DateTime.Now:yyyy-MM-ddHH:mm:ss}");
            writer.WriteLine("Title,Company,Site,Location,Score,Link,ID");

            foreach (var job in jobs)
                writer.WriteLine(
                    $"\"{job.Title}\",\"{job.Company}\",\"{job.Site}\",\"{job.Location}\",\"{job.Score}\",\"{job.Link}\",\"{job.ID}\"");
        }

        public static void SaveOldRecToSave()
        {
            List<Job> oldRecords = new();
            var existingFiles = GetCsvFilesFromDirectory(DEFAULT_PATH);
            foreach (var file in existingFiles)
            {
                var records = ReadJobsFromCsv(file);
                oldRecords.AddRange(records);
            }

            oldRecords = oldRecords.Distinct().ToList();

            SaveJobIndexLine(oldRecords);
        }
    }
}