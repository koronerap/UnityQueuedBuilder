using System.IO;
using System.Text.Json;
using System.Linq;

namespace UnityQueuedBuilder
{
    public class BuildProfile
    {
        public bool Windows { get; set; }
        public bool Android { get; set; }
        public bool IOS { get; set; }
        public bool WebGL { get; set; }
        public bool MacOS { get; set; }
        public bool Linux { get; set; }

        public bool DevBuild { get; set; }
        public bool ScriptDebug { get; set; }
        public bool Profiler { get; set; }
        public bool CleanBuild { get; set; }
        public bool ZipOutput { get; set; }
        public bool OpenFolder { get; set; }
        public bool AutoIncrement { get; set; }
    }

    public static class ProfileManager
    {
        private static string Folder => Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Profiles");

        public static void Save(string name, BuildProfile profile)
        {
            if (!Directory.Exists(Folder)) Directory.CreateDirectory(Folder);
            string json = JsonSerializer.Serialize(profile);
            File.WriteAllText(Path.Combine(Folder, name + ".json"), json);
        }

        public static BuildProfile Load(string name)
        {
            string path = Path.Combine(Folder, name + ".json");
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<BuildProfile>(json);
        }

        public static string[] GetProfiles()
        {
            if (!Directory.Exists(Folder)) return new string[0];
            return Directory.GetFiles(Folder, "*.json").Select(Path.GetFileNameWithoutExtension).ToArray();
        }

        public static void Delete(string name)
        {
            string path = Path.Combine(Folder, name + ".json");
            if (File.Exists(path)) File.Delete(path);
        }
    }
}