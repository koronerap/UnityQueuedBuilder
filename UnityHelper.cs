using System;
using System.IO;
using System.Text.RegularExpressions;

namespace UnityQueuedBuilder
{
    public static class UnityHelper
    {
        // Standart Unity Hub yükleme yolu (Windows için varsayılan)
        private static readonly string DefaultHubPath = @"C:\Program Files\Unity\Hub\Editor";

        public class UnityProjectInfo
        {
            public string Version { get; set; }
            public string EditorPath { get; set; }
            public bool IsEditorInstalled { get; set; }
        }

        public static UnityProjectInfo GetProjectInfo(string projectPath)
        {
            var info = new UnityProjectInfo();
            string versionFilePath = Path.Combine(projectPath, "ProjectSettings", "ProjectVersion.txt");

            if (File.Exists(versionFilePath))
            {
                // Versiyon dosyasını oku
                string content = File.ReadAllText(versionFilePath);
                // Örnek: m_EditorVersion: 2021.3.16f1
                var match = Regex.Match(content, @"m_EditorVersion:\s*([a-zA-Z0-9\.]+)");
                if (match.Success)
                {
                    info.Version = match.Groups[1].Value;

                    // Editör yolunu bulmaya çalış
                    info.EditorPath = FindEditorPath(info.Version);
                    info.IsEditorInstalled = !string.IsNullOrEmpty(info.EditorPath) && File.Exists(info.EditorPath);
                }
            }
            return info;
        }

        private static string FindEditorPath(string version)
        {
            // 1. Varsayılan Hub yolunu kontrol et
            string potentialPath = Path.Combine(DefaultHubPath, version, "Editor", "Unity.exe");
            if (File.Exists(potentialPath))
            {
                return potentialPath;
            }

            // Gelecekte buraya Hub'ın editors.json dosyasını okuma mantığı eklenebilir
            // Şimdilik sadece varsayılan yolu kontrol ediyoruz.
            return null;
        }

        public static bool IsPlatformModuleInstalled(string editorPath, string platform)
        {
            if (string.IsNullOrEmpty(editorPath)) return false;

            string editorFolder = Path.GetDirectoryName(editorPath); // .../Editor
            string playbackEngines = Path.Combine(editorFolder, "Data", "PlaybackEngines");

            // Unity 2019+ yapısında Data klasörü içinde olabilir veya Editor kök dizininde olabilir versiyona göre değişir.
            // Genelde: ...\Editor\2021.3.16f1\Editor\Data\PlaybackEngines\AndroidPlayer

            if (!Directory.Exists(playbackEngines))
            {
                // Bazı versiyonlarda Data bir üst klasörde olabilir
                playbackEngines = Path.Combine(Path.GetDirectoryName(editorFolder), "Data", "PlaybackEngines");
            }

            string moduleFolder = "";
            switch (platform.ToLower())
            {
                case "android":
                    moduleFolder = "AndroidPlayer";
                    break;
                case "windows":
                    moduleFolder = "windowsstandalonesupport"; // Or just check if exe works, but this is the module folder name
                    break;
                case "ios":
                    moduleFolder = "iOSSupport";
                    break;
                case "webgl":
                    moduleFolder = "WebGLSupport";
                    break;
                case "macos":
                    moduleFolder = "MacStandaloneSupport";
                    break;
                case "linux":
                    moduleFolder = "LinuxStandaloneSupport";
                    break;
                default:
                    return false;
            }

            // Note: Windows support is often embedded, but if the folder exists it's definitely there.
            // If it's Windows and folder doesn't exist, we might assume it's there for Windows editors, 
            // but let's check folder existence first. If not found, for Windows we might be lenient
            // HOWEVER, newer Unity versions install Windows support as a module too.

            bool exists = Directory.Exists(Path.Combine(playbackEngines, moduleFolder));

            // Special fallback for Windows on Windows Editor: usually always installed or included
            if (platform.ToLower() == "windows" && !exists) return true;

            return exists;
        }

        public static void OpenUnityHub(string version)
        {
            // Unity Hub'ı açmak için deep link kullanılabilir: unityhub://2021.3.16f1/8463 (changeset gerekebilir)
            // Ya da sadece Hub exe'sini açarız.
            string hubPath = @"C:\Program Files\Unity\Hub\Unity Hub.exe";
            if (File.Exists(hubPath))
            {
                System.Diagnostics.Process.Start(hubPath);
            }
        }

        public static string IncrementProjectVersion(string projectPath)
        {
            try
            {
                string settingsPath = Path.Combine(projectPath, "ProjectSettings", "ProjectSettings.asset");
                if (!File.Exists(settingsPath)) return "Settings file not found";

                string[] lines = File.ReadAllLines(settingsPath);
                string newVersion = "";
                bool changed = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith("bundleVersion:"))
                    {
                        string currentVersion = lines[i].Split(':')[1].Trim();
                        // Try to parse last number
                        string[] parts = currentVersion.Split('.');
                        if (parts.Length > 0 && int.TryParse(parts[parts.Length - 1], out int lastNum))
                        {
                            parts[parts.Length - 1] = (lastNum + 1).ToString();
                            newVersion = string.Join(".", parts);
                            lines[i] = $"  bundleVersion: {newVersion}";
                            changed = true;
                        }
                        break;
                    }
                }

                if (changed)
                {
                    File.WriteAllLines(settingsPath, lines);
                    return newVersion;
                }
            }
            catch { }
            return null;
        }

        public static string GetProjectName(string projectPath)
        {
            try
            {
                string settingsPath = Path.Combine(projectPath, "ProjectSettings", "ProjectSettings.asset");
                if (File.Exists(settingsPath))
                {
                    string[] lines = File.ReadAllLines(settingsPath);
                    foreach (var line in lines)
                    {
                        // productName: My Could Game
                        if (line.Trim().StartsWith("productName:"))
                        {
                            return line.Split(':')[1].Trim();
                        }
                    }
                }
            }
            catch { }
            return "Game"; // Default fallback
        }
    }
}
