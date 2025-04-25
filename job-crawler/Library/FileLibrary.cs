using System.Text.Json;
using System.Xml;
using job_crawler.Models;
using OpenQA.Selenium.DevTools.V131.Console;

namespace job_crawler.Library;

public class FileLibrary
{
    public readonly static string DefaultOutputDirectory = "~/Documents/jobs/";
    public readonly static string DefaultFileName = "crawled-job";

    public static string AskForFilePath()
    {
        while (true)
        {
            Console.Write("Enter the full path to save the file (with or without filename): ");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("ℹ️ Default path will be used: ~/Documents/jobs/");
                input = DefaultOutputDirectory;
            }

            try
            {
                var (folder, fileName) = PathHelper.SplitFolderAndFile(input, DefaultFileName);
                Directory.CreateDirectory(folder);

                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var finalFileName = $"{fileName}-{timestamp}.csv";

                return Path.Combine(folder, finalFileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Invalid path entered:");
                Console.WriteLine(ex.Message);
            }
        }
    }

    public static T? LoadConfig<T>(string path)
    {
        var json = File.ReadAllText(PathHelper.Resolve(path));

        JsonSerializerOptions? options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        return JsonSerializer.Deserialize<T>(json, options);
    }

    public static List<string> GetCsvFilesFromDirectory(string folderPath)
    {
        var csvFiles = new List<string>();
        folderPath = PathHelper.Resolve(folderPath);

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

        path = PathHelper.Resolve(path);

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

            var headers = CsvHelper.ParseLine(headerLine);
            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < headers.Count; i++)
            {
                headerMap[headers[i]] = i;
            }

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var fields = CsvHelper.ParseLine(line);
                if (fields.Count < headers.Count) continue; // skip malformed lines

                jobs.Add(new Job
                {
                    Title = CsvHelper.GetField(fields, headerMap, "Title"),
                    Company = CsvHelper.GetField(fields, headerMap, "Company"),
                    Site = CsvHelper.GetField(fields, headerMap, "Site"),
                    Location = CsvHelper.GetField(fields, headerMap, "Location"),
                    Link = CsvHelper.GetField(fields, headerMap, "Link"),
                    ID = CsvHelper.GetField(fields, headerMap, "ID"),
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error reading jobs from CSV: {ex.Message}");
        }

        return jobs;
    }

    public static class SaveHandler
    {
        public const string DefaultSaveDirectory = "history/";
        public const string DefaultSaveFileName = "save";
        public const string DefaultSavePath = DefaultSaveDirectory + DefaultSaveFileName;

        public static void SaveJobIndexLine(List<Job> jobs, HashSet<string> prevJobs, string path = DefaultSavePath)
        {
            try
            {
                var (folder, fileName) = PathHelper.SplitFolderAndFile(path, DefaultSaveFileName);
                Directory.CreateDirectory(folder);
                var fullpath = Path.Combine(folder, $"{fileName}-{DateTime.Now:yyyyMMdd}.csv");

                // ✅ Add new keys from current job list
                foreach (var job in jobs)
                {
                    prevJobs.Add($"{job.Site}:{job.ID}");
                }

                Console.WriteLine(
                    $"Saving {jobs.Count} job(s) to {fullpath}. Total jobs found today: {prevJobs.Count}");

                // ✅ Ensure folder exists before writing
                Directory.CreateDirectory(folder);

                using var writer = new StreamWriter(fullpath);
                writer.WriteLine(string.Join("|", prevJobs));
            }
            catch (Exception e)
            {
                Console.WriteLine($"❌ Failed to save job index: {e.Message}");
                Console.WriteLine(e);
                throw;
            }
        }

        public static (HashSet<string> prevDayRec, HashSet<string> samedayRec) LoadJobIndexRecords()
        {
            var samedayFile = $"{DefaultSavePath}-{DateTime.Now:yyyyMMdd}.csv";
            var sameDayHash = LoadJobIndexFile(samedayFile);

            var latestFile = Directory
                .GetFiles(PathHelper.Resolve(DefaultSaveDirectory))
                .Where(f => Path.GetFileName(f).StartsWith(DefaultSaveFileName) && !string.Equals(Path.GetFileName(f), Path.GetFileName(samedayFile)))
                .OrderByDescending(File.GetCreationTimeUtc)
                .FirstOrDefault();

            var prevDayHash = latestFile != null
                ? LoadJobIndexFile(latestFile)
                : new HashSet<string>();

            if (latestFile == null)
                Console.WriteLine("⚠️ Previous records not found.");

            return (prevDayHash, sameDayHash);
        }

        public static HashSet<string> LoadJobIndexFile(string path = DefaultSavePath)
        {
            path = PathHelper.Resolve(path);
            Console.WriteLine($"📂 Loading records: {path}");

            var set = new HashSet<string>();

            if (!File.Exists(path))
            {
                Console.WriteLine("⚠️ File not found.");
                return set;
            }

            var content = File.ReadAllText(path);
            var parts = content.Split('|', StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
                set.Add(part.Trim());

            Console.WriteLine($"✅ Loaded {set.Count} records.");
            return set;
        }

        public static void SaveJobsToCsv(List<Job> jobs, string filename)
        {
            if (string.IsNullOrEmpty(filename)) filename = DefaultFileName;
            using var writer = new StreamWriter(filename);
            writer.WriteLine($"{DateTime.Now:yyyy-MM-ddHH:mm:ss}");
            writer.WriteLine("Title,Company,Site,Location,Score,Link,ID");

            foreach (var job in jobs)
            {
                var fields = new[]
                {
                    CsvHelper.Escape(job.Title),
                    CsvHelper.Escape(job.Company),
                    job.Site,
                    CsvHelper.Escape(job.Location),
                    job.Score.ToString("F2"),
                    CsvHelper.Escape(job.Link),
                    job.ID
                };
                writer.WriteLine(string.Join(',', fields));
            }
        }

        public static void SaveOldRecToSave()
        {
            List<Job> oldRecords = new();
            var existingFiles = GetCsvFilesFromDirectory(DefaultOutputDirectory);
            foreach (var file in existingFiles)
            {
                var records = ReadJobsFromCsv(file);
                oldRecords.AddRange(records);
            }

            oldRecords = oldRecords.Distinct().ToList();

            SaveJobIndexLine(oldRecords, new HashSet<string>());
        }
    }

    public static class PathHelper
    {
        public static string ExpandHome(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return path;
            return path.StartsWith("~")
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    path[1..].TrimStart(Path.DirectorySeparatorChar))
                : path;
        }

        public static string Resolve(string inputPath)
        {
            var expanded = ExpandHome(inputPath);
            return Path.IsPathRooted(expanded)
                ? Path.GetFullPath(expanded)
                : Path.Combine(AppContext.BaseDirectory, expanded);
        }

        public static (string folder, string fileName) SplitFolderAndFile(string fullPath, string defaultFileName)
        {
            var resolved = Resolve(fullPath);
            if (Directory.Exists(resolved) || resolved.EndsWith(Path.DirectorySeparatorChar))
                return (resolved, defaultFileName);

            return (Path.GetDirectoryName(resolved) ?? "", Path.GetFileName(resolved));
        }
    }

    public static class CsvHelper
    {
        public static List<string> ParseLine(string line)
        {
            var fields = new List<string>();
            var current = "";
            bool inQuotes = false;

            foreach (char c in line)
            {
                if (c == '\"') inQuotes = !inQuotes;
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(current.Trim());
                    current = "";
                }
                else current += c;
            }

            fields.Add(current.Trim());
            return fields;
        }

        // Helper to get a value by column name
        internal static string GetField(List<string> fields, Dictionary<string, int> map, string key)
        {
            return map.TryGetValue(key, out int index) && index < fields.Count
                ? fields[index].Trim()
                : "";
        }

        public static string Escape(string value) =>
            string.IsNullOrEmpty(value) ? string.Empty : $"\"{value.Replace("\"", "\"\"")}\"";
    }
}