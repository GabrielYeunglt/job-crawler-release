using job_crawler.Models;

namespace job_crawler.Library;

public struct StaticValue
{
    public enum JobSites
    {
        All,
        Indeed,
        LinkedIn,
        Glassdoor
    }


    public static List<Keyword> GetKeywords()
    {
        var keywords = new List<Keyword>
        {
            new("C#"),
            new(".NET"),
            new("ASP.NET Core"),
            new("React.js"),
            new("Node.js"),
            new("Redux"),
            new("SQL"),
            new("MSSQL"),
            new("MongoDB"),
            new("Azure"),
            new("Azure Functions"),
            new("Azure DevOps"),
            new("Power Platform"),
            new("Power Apps"),
            new("Power Automate"),
            new("Git"),
            new("GitHub"),
            new("CI/CD"),
            new("Microservices"),
            new("RESTful API"),
            new("Docker"),
            new("Kubernetes"),
            new("NoSQL"),
            new("Linux"),
            new("HRMS"),
            new("AngularJS"),
            new("Object-Oriented Programming"),
            new("DevOps"),
            new("Cloud Integration"),
            new("Performance Optimization"),
            new("Enterprise Security")
        };

        // Adding synonyms using AddSynonyms method
        keywords[0].AddSynonyms(new List<string> { "CSharp", "C Sharp", ".NET C#", "C# Programming", "C# Language" });
        keywords[1].AddSynonyms(new List<string>
            { "DotNet", ".NET Framework", ".NET Core", ".NET 5+", "Microsoft .NET" });
        keywords[2].AddSynonyms(new List<string>
            { "ASP.NET", "ASP.NET MVC", "ASP.NET Web API", ".NET Web Development", "Microsoft Web Framework" });
        keywords[3].AddSynonyms(new List<string>
            { "React", "ReactJS", "React Framework", "React UI Library", "JSX", "React Components" });
        keywords[4].AddSynonyms(new List<string>
            { "NodeJS", "Node.js Server", "Node Scripting", "JavaScript Runtime" });
        keywords[5].AddSynonyms(new List<string> { "ReduxJS", "React Redux", "State Management" });
        keywords[6].AddSynonyms(new List<string>
        {
            "Structured Query Language", "SQL Database", "SQL Querying", "SQL Server", "SQL Scripting",
            "Relational Database"
        });
        keywords[7].AddSynonyms(new List<string>
        {
            "Microsoft SQL Server", "SQL Server", "MS SQL", "T-SQL", "Transact-SQL",
            "SQL Server Management Studio (SSMS)"
        });
        keywords[8].AddSynonyms(new List<string> { "MongoDB Atlas", "Mongo NoSQL", "Mongo Query", "Mongo Shell" });
        keywords[9].AddSynonyms(new List<string>
            { "Microsoft Azure", "Azure Cloud", "Azure Services", "Azure Cloud Computing" });
        keywords[10].AddSynonyms(new List<string> { "Azure Serverless", "Azure Logic Apps", "Azure Function Apps" });
        keywords[11].AddSynonyms(new List<string> { "Azure Pipelines", "Azure CI/CD", "Azure Deployment" });
        keywords[12].AddSynonyms(new List<string>
            { "Microsoft Power Platform", "PowerApps", "Power BI", "Power Automate", "Power Virtual Agents" });
        keywords[13].AddSynonyms(new List<string>
            { "Microsoft PowerApps", "PowerApps Low-Code", "PowerApps Development" });
        keywords[14].AddSynonyms(new List<string>
            { "Microsoft Power Automate", "Power Automate Flow", "Power Automate Workflows" });
        keywords[15].AddSynonyms(new List<string> { "Git Source Control", "Git Versioning", "Git Commands" });
        keywords[16].AddSynonyms(new List<string> { "GitHub Repositories", "GitHub Actions", "GitHub CI/CD" });
        keywords[17].AddSynonyms(new List<string>
            { "Continuous Integration", "Continuous Deployment", "CI/CD Pipeline", "DevOps CI/CD" });
        keywords[18].AddSynonyms(new List<string>
            { "Microservices Architecture", "Distributed Systems", "Service-Oriented Architecture (SOA)" });
        keywords[19].AddSynonyms(new List<string> { "REST API", "Web API", "API Development", "RESTful Services" });
        keywords[20].AddSynonyms(new List<string> { "Docker Containers", "Containerization", "Docker Compose" });
        keywords[21].AddSynonyms(new List<string> { "K8s", "K8s Orchestration", "Kubernetes Cluster" });
        keywords[22].AddSynonyms(new List<string> { "NoSQL Database", "Non-Relational Database", "NoSQL Storage" });
        keywords[23].AddSynonyms(new List<string> { "Unix", "Linux Server", "Linux OS", "Linux Commands" });
        keywords[24].AddSynonyms(new List<string>
            { "Human Resource Management System", "HR Management Software", "HRMS Application" });
        keywords[25].AddSynonyms(new List<string> { "Angular", "Angular Framework", "Angular Frontend" });
        keywords[26].AddSynonyms(new List<string> { "OOP", "Object-Oriented Design", "OOP Principles" });
        keywords[27].AddSynonyms(new List<string>
            { "DevOps Practices", "DevOps Automation", "Infrastructure as Code (IaC)" });
        keywords[28].AddSynonyms(new List<string> { "Cloud Solutions", "Cloud Migration", "Cloud Computing" });
        keywords[29].AddSynonyms(new List<string>
            { "System Performance", "Code Optimization", "Efficient Algorithms" });
        keywords[30].AddSynonyms(new List<string> { "Cybersecurity", "Enterprise IT Security", "Cloud Security" });

        return keywords;
    }
}