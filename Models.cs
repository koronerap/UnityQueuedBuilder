using System.Collections.Generic;

namespace UnityQueuedBuilder
{
    public enum BuildPlatform
    {
        Windows64,
        Android,
        iOS,
        WebGL,
        MacOS,
        Linux
    }

    public class BuildJob
    {
        public string ProjectPath { get; set; }
        public string UnityEditorPath { get; set; }
        public BuildPlatform Platform { get; set; }
        public string OutputPath { get; set; }
        public bool DevelopmentBuild { get; set; }
        public bool ScriptDebugging { get; set; }
        public bool AutoConnectProfiler { get; set; }
        public bool CleanBuild { get; set; }
        public string ExeName { get; set; } = "Game";
        public string Status { get; set; } = "Waiting";
    }
}
