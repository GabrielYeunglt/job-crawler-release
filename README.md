# 🕷️ Job Crawler Service

This project automates the process of crawling and analyzing job postings from multiple job sites (e.g., Indeed and LinkedIn) using Selenium and custom parsers.

---

## 📌 Features

- Automated login and crawling via Selenium WebDriver.
- Modular site-specific parsing using `IJobSiteParser`.
- Intelligent job scoring using `JobAnalyzeService`.
- Deduplication of job records to avoid reprocessing.
- CSV export of stable job listings.
- Configurable crawling target file path.

---

## 🚀 How It Works

The main entry point is:

```csharp
public void Crawl(string filepath = "crawled")
```

### This method:
1. **Reads existing records** from past CSV files to avoid reprocessing duplicate jobs.
2. **Crawls Indeed and LinkedIn** job listings using custom parser implementations:
   - `IndeedJobSiteParser`
   - `LinkedInJobSiteParser`
3. **Deduplicates** and filters new jobs based on ID and existence in previous data.
4. **Enhances each job listing** with detailed content and a relevance score.
5. **Sorts** all jobs by score (descending).
6. **Exports** results to a CSV file (`crawled` by default, or a custom filename).

---

## 🛠️ Prerequisites

- .NET 6 or newer
- Chrome browser installed
- ChromeDriver available in your system path

---

## 📂 Output Format

The output CSV includes:

- Timestamp
- Job Title
- Company
- Site (e.g., Indeed, LinkedIn)
- Location
- Score
- Link
- Unique ID

---

## ⚙️ Usage

Run the crawler from code like this:

```csharp
using var service = new SelJobCrawlerService();
service.Crawl("output.csv"); // Optional file name
```

---

## 📁 Configs

- Place `.csv` records from previous runs in the default `FileLibrary.DEFAULT_PATH` directory to enable deduplication.
- You can customize the parsers to target other job sites by implementing `IJobSiteParser`.

---

## ⚠️ Disclaimer

This project is intended for educational and personal use only.

JobCrawler may interact with third-party websites (e.g., job boards) in ways that may violate their Terms of Service (ToS). It is the user's responsibility to ensure that their use of this tool complies with the ToS of any target site.

The author assumes no liability for misuse or damages resulting from the use of this tool. Use at your own risk.

If you are the owner or representative of a site and have concerns about this project, please open an issue or contact the maintainer directly.

