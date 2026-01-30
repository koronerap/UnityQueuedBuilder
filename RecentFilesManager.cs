using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Win32; // For Registry

namespace UnityQueuedBuilder
{
    public class RecentProject
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
    }

    public static class RecentFilesManager
    {
        private static string FilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RecentProjects.json");
        private const int MaxRecents = 15;

        public static List<RecentProject> Load()
        {
            var rawPaths = new List<string>();

            // 1. Load from our JSON
            if (File.Exists(FilePath))
            {
                try
                {
                    string json = File.ReadAllText(FilePath);
                    var saved = JsonSerializer.Deserialize<List<string>>(json);
                    if (saved != null) rawPaths.AddRange(saved);
                }
                catch { }
            }

            // 2. Load from Unity Registry (Hub/Editor History)
            var unityRecents = GetUnityRecentProjects();
            foreach (var path in unityRecents)
            {
                if (!rawPaths.Contains(path, StringComparer.OrdinalIgnoreCase) && Directory.Exists(path))
                {
                    rawPaths.Add(path);
                }
            }

            // 3. Load from Unity Hub JSONs
            var hubRecents = GetUnityHubProjects();
            foreach (var path in hubRecents)
            {
                if (!rawPaths.Contains(path, StringComparer.OrdinalIgnoreCase) && Directory.Exists(path))
                {
                    rawPaths.Add(path);
                }
            }

            // Convert to RecentProject objects
            var projectList = new List<RecentProject>();
            foreach (var path in rawPaths)
            {
                if (Directory.Exists(path))
                {
                    string name = UnityHelper.GetProjectName(path);
                    if (string.IsNullOrEmpty(name)) name = new DirectoryInfo(path).Name;

                    projectList.Add(new RecentProject { Name = name, Path = path });
                }
            }

            return projectList;
        }

        private static List<string> GetUnityHubProjects()
        {
            var results = new List<string>();
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string hubPath = Path.Combine(appData, "UnityHub");

                // Possible files
                string[] candidates = { "projects-v1.json", "projects.json" };

                foreach (var filename in candidates)
                {
                    string file = Path.Combine(hubPath, filename);
                    if (File.Exists(file))
                    {
                        try
                        {
                            string json = File.ReadAllText(file);
                            using (JsonDocument doc = JsonDocument.Parse(json))
                            {
                                if (doc.RootElement.TryGetProperty("data", out JsonElement data))
                                {
                                    foreach (var property in data.EnumerateObject())
                                    {
                                        // Key or 'path' property might be the path
                                        string? p = null;
                                        if (property.Value.TryGetProperty("path", out JsonElement pathEl))
                                        {
                                            p = pathEl.GetString();
                                        }

                                        if (!string.IsNullOrWhiteSpace(p)) results.Add(p);
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
            return results;
        }

        private static List<string> GetUnityRecentProjects()
        {
            var results = new List<string>();
            try
            {
                // Unity 5.x+ stores recent projects in Registry
                // HKCU\Software\Unity Technologies\Unity Editor 5.x
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Unity Technologies\Unity Editor 5.x"))
                {
                    if (key != null)
                    {
                        var valueNames = key.GetValueNames()
                                            .Where(n => n.StartsWith("RecentlyUsedProjectPaths-"))
                                            .OrderBy(n =>
                                            {
                                                // Extract number: RecentlyUsedProjectPaths-0
                                                var parts = n.Split('-');
                                                if (parts.Length > 1 && int.TryParse(parts[1], out int idx)) return idx;
                                                return 999;
                                            });

                        foreach (var name in valueNames)
                        {
                            var val = key.GetValue(name);
                            string path = "";

                            if (val is byte[] bytes)
                            {
                                // Often null-terminated
                                path = System.Text.Encoding.UTF8.GetString(bytes).TrimEnd('\0');
                            }
                            else if (val is string str)
                            {
                                path = str;
                            }

                            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                            {
                                results.Add(path);
                            }
                        }
                    }
                }
            }
            catch { }
            return results;
        }

        public static void Add(string path)
        {
            var rawPaths = new List<string>();
            if (File.Exists(FilePath))
            {
                try
                {
                    string json = File.ReadAllText(FilePath);
                    var saved = JsonSerializer.Deserialize<List<string>>(json);
                    if (saved != null) rawPaths.AddRange(saved);
                }
                catch { }
            }

            // Remove if exists
            rawPaths.RemoveAll(x => x.Equals(path, StringComparison.OrdinalIgnoreCase));

            // Insert at top
            rawPaths.Insert(0, path);

            // Limit
            if (rawPaths.Count > MaxRecents)
                rawPaths = rawPaths.Take(MaxRecents).ToList();

            try
            {
                string json = JsonSerializer.Serialize(rawPaths, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
            catch { }
        }
    }
}
