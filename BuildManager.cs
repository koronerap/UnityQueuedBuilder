using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace UnityQueuedBuilder
{
    public class BuildManager
    {
        public event Action<string> OnLogReceived;

        public async Task StartBuildQueue(List<BuildJob> jobs)
        {
            foreach (var job in jobs)
            {
                OnLogReceived?.Invoke($">>> Starting Build for: {job.Platform}");
                bool result = await BuildAsync(job);
                if (!result)
                {
                    OnLogReceived?.Invoke($"!!! Build Failed for: {job.Platform}. Stopping queue.");
                    throw new Exception($"Build failed for {job.Platform}");
                }
                OnLogReceived?.Invoke($">>> Build Completed for: {job.Platform}");
            }
        }

        public async Task<bool> BuildAsync(BuildJob job)
        {
            string logFile = Path.Combine(job.OutputPath, $"build_log_{job.Platform}.txt");

            // Delete existing log file to ensure we read fresh logs
            if (File.Exists(logFile))
            {
                try { File.Delete(logFile); } catch { /* Ignore */ }
            }

            string args = $"-batchmode -quit -projectPath \"{job.ProjectPath}\" -logFile \"{logFile}\"";

            // Common options
            if (job.DevelopmentBuild) args += " -development";
            if (job.ScriptDebugging) args += " -scriptDebug";
            if (job.AutoConnectProfiler) args += " -profiler";
            if (job.CleanBuild) args += " -clean";

            // Platform specific arguments
            string exeName = string.IsNullOrWhiteSpace(job.ExeName) ? "Game" : job.ExeName;

            switch (job.Platform)
            {
                case BuildPlatform.Windows64:
                    string exePath = Path.Combine(job.OutputPath, "Windows", $"{exeName}.exe");
                    // We now use executeMethod for Windows too, to support options easily
                    await InjectBuildScript(job.ProjectPath);
                    args += $" -executeMethod UnityQueuedBuilderHelper.BuildWindows -outputBuildPath \"{exePath}\"";
                    break;

                case BuildPlatform.Android:
                    string apkPath = Path.Combine(job.OutputPath, "Android", $"{exeName}.apk");
                    await InjectBuildScript(job.ProjectPath);
                    args += $" -executeMethod UnityQueuedBuilderHelper.BuildAndroid -outputBuildPath \"{apkPath}\"";
                    break;

                case BuildPlatform.iOS:
                    string iosPath = Path.Combine(job.OutputPath, "iOS");
                    await InjectBuildScript(job.ProjectPath);
                    args += $" -executeMethod UnityQueuedBuilderHelper.BuildIOS -outputBuildPath \"{iosPath}\"";
                    break;

                case BuildPlatform.WebGL:
                    string webglPath = Path.Combine(job.OutputPath, "WebGL");
                    await InjectBuildScript(job.ProjectPath);
                    args += $" -executeMethod UnityQueuedBuilderHelper.BuildWebGL -outputBuildPath \"{webglPath}\"";
                    break;

                case BuildPlatform.MacOS:
                    string macPath = Path.Combine(job.OutputPath, "MacOS", $"{exeName}.app");
                    await InjectBuildScript(job.ProjectPath);
                    args += $" -executeMethod UnityQueuedBuilderHelper.BuildMacOS -outputBuildPath \"{macPath}\"";
                    break;

                case BuildPlatform.Linux:
                    string linuxPath = Path.Combine(job.OutputPath, "Linux", $"{exeName}.x86_64");
                    await InjectBuildScript(job.ProjectPath);
                    args += $" -executeMethod UnityQueuedBuilderHelper.BuildLinux -outputBuildPath \"{linuxPath}\"";
                    break;
            }

            job.Status = "Building...";
            OnLogReceived?.Invoke($"Starting build for {job.Platform}...");
            OnLogReceived?.Invoke($"Log file: {logFile}");

            return await RunProcessWithLogMonitoringAsync(job.UnityEditorPath, args, logFile);
        }

        private async Task<bool> RunProcessWithLogMonitoringAsync(string fileName, string args, string logFilePath)
        {
            var tcs = new TaskCompletionSource<bool>();

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            process.Exited += (sender, e) =>
            {
                tcs.SetResult(process.ExitCode == 0);
                process.Dispose();
            };

            try
            {
                process.Start();

                // Wait for the log file to be created
                int retryCount = 0;
                while (!File.Exists(logFilePath) && retryCount < 20 && !process.HasExited)
                {
                    await Task.Delay(500);
                    retryCount++;
                }

                if (File.Exists(logFilePath))
                {
                    using (var fs = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs))
                    {
                        while (!process.HasExited)
                        {
                            string line = await sr.ReadLineAsync();
                            if (line != null)
                            {
                                OnLogReceived?.Invoke(line);
                            }
                            else
                            {
                                // No new lines, wait a bit
                                await Task.Delay(100);
                            }
                        }

                        // Read remaining lines after exit
                        string rest = await sr.ReadToEndAsync();
                        if (!string.IsNullOrEmpty(rest))
                        {
                            OnLogReceived?.Invoke(rest);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnLogReceived?.Invoke($"Monitor Error: {ex.Message}");
            }

            return await tcs.Task;
        }

        private async Task InjectBuildScript(string projectPath)
        {
            string editorFolder = Path.Combine(projectPath, "Assets", "Editor");
            if (!Directory.Exists(editorFolder))
            {
                Directory.CreateDirectory(editorFolder);
            }

            string scriptContent = @"
using UnityEditor;
using System.Linq;
using System;

public class UnityQueuedBuilderHelper
{
    public static void BuildAndroid()
    {
        PerformBuild(""AndroidBuild.apk"", BuildTarget.Android);
    }

    public static void BuildIOS()
    {
        PerformBuild(""iOSBuild"", BuildTarget.iOS);
    }

    public static void BuildWebGL()
    {
        PerformBuild(""WebGLBuild"", BuildTarget.WebGL);
    }

    public static void BuildMacOS()
    {
        PerformBuild(""MacOS.app"", BuildTarget.StandaloneOSX);
    }

    public static void BuildLinux()
    {
        PerformBuild(""LinuxBuild.x86_64"", BuildTarget.StandaloneLinux64);
    }

    public static void BuildWindows()
    {
        PerformBuild(""Windows/Game.exe"", BuildTarget.StandaloneWindows64);
    }

    private static void PerformBuild(string defaultPath, BuildTarget target)
    {
        string outputPath = defaultPath;
        BuildOptions options = BuildOptions.None;

        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == ""-outputBuildPath"" && i + 1 < args.Length)
            {
                outputPath = args[i + 1];
            }
            if (args[i] == ""-development"") options |= BuildOptions.Development;
            if (args[i] == ""-scriptDebug"") options |= BuildOptions.AllowDebugging;
            if (args[i] == ""-profiler"") options |= BuildOptions.ConnectWithProfiler;
            if (args[i] == ""-clean"") options |= BuildOptions.CleanBuildCache;
        }

        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        BuildPipeline.BuildPlayer(scenes, outputPath, target, options);
    }
}
";
            string scriptPath = Path.Combine(editorFolder, "UnityQueuedBuilderHelper.cs");
            if (!File.Exists(scriptPath))
            {
                await File.WriteAllTextAsync(scriptPath, scriptContent);
            }
        }
    }
}
