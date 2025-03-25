using System.Text.Json;
using job_crawler.Models;

namespace job_crawler.Library;

public class FileLibrary
{
    public readonly static string DEFAULT_PATH = "~/Documents/jobs/";

    public static string AskForFilePath()
    {
        var defaultFileName = "crawled-job";
        var fullPath = "";
        var isValidPath = false;
        var folder = "";
        var fileName = "";

        while (!isValidPath)
        {
            Console.Write("Enter the full path to save the file (with or without filename): ");
            fullPath = Console.ReadLine()?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(fullPath))
            {
                Console.WriteLine("Default path would be used. ~/Documents/jobs/");
                fullPath = DEFAULT_PATH;
            }

            // Expand ~ to home directory if needed
            fullPath = ExpandHome(fullPath);

            try
            {
                fullPath = Path.GetFullPath(fullPath);

                // If it's an existing folder or ends with a slash, use default file name
                if (Directory.Exists(fullPath) || fullPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                {
                    folder = fullPath;
                    fileName = defaultFileName;
                }
                else
                {
                    folder = Path.GetDirectoryName(fullPath) ?? "";
                    fileName = Path.GetFileName(fullPath);

                    if (string.IsNullOrWhiteSpace(fileName))
                        fileName = defaultFileName;
                }

                if (!Directory.Exists(folder))
                {
                    Console.WriteLine("⚠️ Folder does not exist. Creating it...");
                    Directory.CreateDirectory(folder);
                }

                isValidPath = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Invalid path: {ex.Message}");
            }
        }

        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var finalFileName = $"{fileName}-{timestamp}.csv";
        return Path.Combine(folder, finalFileName);
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
            using (var reader = new StreamReader(path))
            {
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
            if (c == '\"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(current.Trim());
                current = "";
            }
            else
            {
                current += c;
            }
        }

        fields.Add(current.Trim()); // add last field
        return fields;
    }
}