using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms; // Requires UseWindowsForms=true in csproj
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Documents;
using System.Windows.Threading;
using System.IO.Compression; // For ZipFile
using System.Media; // For SystemSound

namespace UnityQueuedBuilder
{
    public partial class MainWindow : Window
    {
        private UnityHelper.UnityProjectInfo _currentProjectInfo;
        private BuildManager _buildManager;
        private string _selectedProjectPath;
        private string _selectedOutputPath;
        private DispatcherTimer _buildTimer;
        private DateTime _buildStartTime;

        public MainWindow()
        {
            InitializeComponent();
            _buildManager = new BuildManager();
            _buildManager.OnLogReceived += Log;

            _buildTimer = new DispatcherTimer();
            _buildTimer.Interval = TimeSpan.FromSeconds(1);
            _buildTimer.Tick += (s, e) =>
            {
                var elapsed = DateTime.Now - _buildStartTime;
                TxtTimer.Text = elapsed.ToString(@"hh\:mm\:ss");
            };

            // Load Profiles
            RefreshProfiles();

            // Load Recents
            RefreshRecents();

            // Event Subscriptions for Validation
            ChkWindows.Checked += (s, e) => ValidateRequirements();
            ChkWindows.Unchecked += (s, e) => ValidateRequirements();
            ChkAndroid.Checked += (s, e) => ValidateRequirements();
            ChkAndroid.Unchecked += (s, e) => ValidateRequirements();
            ChkIOS.Checked += (s, e) => ValidateRequirements();
            ChkIOS.Unchecked += (s, e) => ValidateRequirements();
            ChkWebGL.Checked += (s, e) => ValidateRequirements();
            ChkWebGL.Unchecked += (s, e) => ValidateRequirements();
            ChkMacOS.Checked += (s, e) => ValidateRequirements();
            ChkMacOS.Unchecked += (s, e) => ValidateRequirements();
            ChkLinux.Checked += (s, e) => ValidateRequirements();
            ChkLinux.Unchecked += (s, e) => ValidateRequirements();
        }

        private void RefreshProfiles()
        {
            CmbProfiles.Items.Clear();
            var profiles = ProfileManager.GetProfiles();
            foreach (var p in profiles) CmbProfiles.Items.Add(p);
        }

        private void RefreshRecents()
        {
            CmbRecentProjects.Items.Clear();
            var recents = RecentFilesManager.Load();
            foreach (var r in recents) CmbRecentProjects.Items.Add(r);
        }

        private void CmbRecentProjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbRecentProjects.SelectedValue is string path && !string.IsNullOrWhiteSpace(path))
            {
                if (System.IO.Directory.Exists(path))
                {
                    _selectedProjectPath = path;

                    // Reset or set default output path if not manually chosen yet
                    if (string.IsNullOrEmpty(_selectedOutputPath))
                    {
                        TxtOutputPath.Text = System.IO.Path.Combine(_selectedProjectPath, "Builds");
                    }

                    LoadProjectInfo(_selectedProjectPath);

                    // Auto-detect project name
                    string projName = UnityHelper.GetProjectName(_selectedProjectPath);
                    TxtExeName.Text = projName;
                }
            }
        }

        private void BtnSaveProfile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("Enter Profile Name:", "Save Profile");
            if (dialog.ShowDialog() == true)
            {
                var profile = new BuildProfile
                {
                    Windows = ChkWindows.IsChecked == true,
                    Android = ChkAndroid.IsChecked == true,
                    IOS = ChkIOS.IsChecked == true,
                    WebGL = ChkWebGL.IsChecked == true,
                    MacOS = ChkMacOS.IsChecked == true,
                    Linux = ChkLinux.IsChecked == true,
                    DevBuild = ChkDevBuild.IsChecked == true,
                    ScriptDebug = ChkScriptDebug.IsChecked == true,
                    Profiler = ChkProfiler.IsChecked == true,
                    CleanBuild = ChkCleanBuild.IsChecked == true,
                    ZipOutput = ChkZipOutput.IsChecked == true,
                    OpenFolder = ChkOpenFolder.IsChecked == true,
                    AutoIncrement = ChkAutoIncrement.IsChecked == true
                };
                ProfileManager.Save(dialog.InputText, profile);
                RefreshProfiles();
                CmbProfiles.SelectedItem = dialog.InputText;
            }
        }

        private void BtnLoadProfile_Click(object sender, RoutedEventArgs e)
        {
            if (CmbProfiles.SelectedItem is string name)
            {
                var profile = ProfileManager.Load(name);
                if (profile != null)
                {
                    ChkWindows.IsChecked = profile.Windows;
                    ChkAndroid.IsChecked = profile.Android;
                    ChkIOS.IsChecked = profile.IOS;
                    ChkWebGL.IsChecked = profile.WebGL;
                    ChkMacOS.IsChecked = profile.MacOS;
                    ChkLinux.IsChecked = profile.Linux;

                    ChkDevBuild.IsChecked = profile.DevBuild;
                    ChkScriptDebug.IsChecked = profile.ScriptDebug;
                    ChkProfiler.IsChecked = profile.Profiler;
                    ChkCleanBuild.IsChecked = profile.CleanBuild;
                    ChkZipOutput.IsChecked = profile.ZipOutput;
                    ChkOpenFolder.IsChecked = profile.OpenFolder;
                    ChkAutoIncrement.IsChecked = profile.AutoIncrement;
                }
            }
        }

        private void BtnDeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            if (CmbProfiles.SelectedItem is string name)
            {
                ProfileManager.Delete(name);
                RefreshProfiles();
            }
        }

        private void BtnChooseProject_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select Unity Project Folder";
                dialog.UseDescriptionForTitle = true;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _selectedProjectPath = dialog.SelectedPath;

                    // Add to Recents
                    RecentFilesManager.Add(_selectedProjectPath);
                    RefreshRecents();

                    // Select the newly added item
                    foreach (RecentProject rp in CmbRecentProjects.Items)
                    {
                        if (rp.Path.Equals(_selectedProjectPath, StringComparison.OrdinalIgnoreCase))
                        {
                            CmbRecentProjects.SelectedItem = rp;
                            break;
                        }
                    }

                    // Reset or set default output path if not manually chosen yet
                    if (string.IsNullOrEmpty(_selectedOutputPath))
                    {
                        TxtOutputPath.Text = System.IO.Path.Combine(_selectedProjectPath, "Builds");
                    }

                    LoadProjectInfo(_selectedProjectPath);

                    // Auto-detect project name
                    string projName = UnityHelper.GetProjectName(_selectedProjectPath);
                    TxtExeName.Text = projName;
                }
            }
        }

        private void ChkDevBuild_Checked(object sender, RoutedEventArgs e)
        {
            if (ChkDevBuild.IsChecked == true)
            {
                ChkScriptDebug.IsEnabled = true;
                ChkProfiler.IsEnabled = true;
            }
            else
            {
                ChkScriptDebug.IsEnabled = false;
                ChkScriptDebug.IsChecked = false;
                ChkProfiler.IsEnabled = false;
                ChkProfiler.IsChecked = false;
            }
        }

        private void BtnChooseOutput_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select Output Folder for Builds";
                dialog.UseDescriptionForTitle = true;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _selectedOutputPath = dialog.SelectedPath;
                    TxtOutputPath.Text = _selectedOutputPath;
                }
            }
        }

        private void LoadProjectInfo(string path)
        {
            Log($"Analyzing project at: {path}");
            _currentProjectInfo = UnityHelper.GetProjectInfo(path);

            if (!string.IsNullOrEmpty(_currentProjectInfo.Version))
            {
                LblUnityVersion.Text = _currentProjectInfo.Version;
                Log($"Detected Unity Version: {_currentProjectInfo.Version}");

                if (_currentProjectInfo.IsEditorInstalled)
                {
                    LblEditorStatus.Text = "Installed";
                    LblEditorStatus.Foreground = System.Windows.Media.Brushes.Green;
                    BtnInstallHub.Visibility = Visibility.Collapsed;
                }
                else
                {
                    LblEditorStatus.Text = "Not Installed";
                    LblEditorStatus.Foreground = System.Windows.Media.Brushes.Red;
                    Log("ERROR: Required Unity Editor is not installed.");
                    BtnInstallHub.Visibility = Visibility.Visible;
                }
            }
            else
            {
                LblUnityVersion.Text = "Unknown";
                LblEditorStatus.Text = "(Invalid Project)";
                Log("Could not detect Unity version. Is this a valid project folder?");
            }

            ValidateRequirements();
        }

        private void ValidateRequirements()
        {
            BtnBuild.IsEnabled = false;
            LblPlatformWarnings.Text = "";

            if (_currentProjectInfo == null || string.IsNullOrEmpty(_currentProjectInfo.Version))
                return;

            if (!_currentProjectInfo.IsEditorInstalled)
            {
                LblPlatformWarnings.Text = "Install the Unity Editor first.";
                return;
            }

            bool valid = true;
            string warnings = "";

            if (ChkAndroid.IsChecked == true)
            {
                if (!UnityHelper.IsPlatformModuleInstalled(_currentProjectInfo.EditorPath, "Android"))
                {
                    warnings += "Missing Android Module! Please install via Unity Hub.\n";
                    valid = false;
                    BtnInstallHub.Visibility = Visibility.Visible;
                }
            }

            if (ChkWindows.IsChecked == true)
            {
                if (!UnityHelper.IsPlatformModuleInstalled(_currentProjectInfo.EditorPath, "Windows"))
                {
                    // Optionally warn
                }
            }

            if (ChkIOS.IsChecked == true)
            {
                if (!UnityHelper.IsPlatformModuleInstalled(_currentProjectInfo.EditorPath, "iOS"))
                {
                    warnings += "Missing iOS Module! Please install via Unity Hub.\n";
                    valid = false;
                    BtnInstallHub.Visibility = Visibility.Visible;
                }
            }

            if (ChkWebGL.IsChecked == true)
            {
                if (!UnityHelper.IsPlatformModuleInstalled(_currentProjectInfo.EditorPath, "WebGL"))
                {
                    warnings += "Missing WebGL Module! Please install via Unity Hub.\n";
                    valid = false;
                    BtnInstallHub.Visibility = Visibility.Visible;
                }
            }

            if (ChkMacOS.IsChecked == true)
            {
                if (!UnityHelper.IsPlatformModuleInstalled(_currentProjectInfo.EditorPath, "MacOS"))
                {
                    warnings += "Missing MacOS Module! Please install via Unity Hub.\n";
                    valid = false;
                    BtnInstallHub.Visibility = Visibility.Visible;
                }
            }

            if (ChkLinux.IsChecked == true)
            {
                if (!UnityHelper.IsPlatformModuleInstalled(_currentProjectInfo.EditorPath, "Linux"))
                {
                    warnings += "Missing Linux Module! Please install via Unity Hub.\n";
                    valid = false;
                    BtnInstallHub.Visibility = Visibility.Visible;
                }
            }

            if (!string.IsNullOrEmpty(warnings))
            {
                LblPlatformWarnings.Text = warnings;
                valid = false;
            }
            else
            {
                if (ChkAndroid.IsChecked == false &&
                    ChkWindows.IsChecked == false &&
                    ChkIOS.IsChecked == false &&
                    ChkWebGL.IsChecked == false &&
                    ChkMacOS.IsChecked == false &&
                    ChkLinux.IsChecked == false)
                {
                    valid = false;
                }
            }

            BtnBuild.IsEnabled = valid;
        }

        private void BtnInstallHub_Click(object sender, RoutedEventArgs e)
        {
            if (_currentProjectInfo != null && !string.IsNullOrEmpty(_currentProjectInfo.Version))
            {
                UnityHelper.OpenUnityHub(_currentProjectInfo.Version);
            }
            else
            {
                UnityHelper.OpenUnityHub("");
            }
        }

        private async void BtnBuild_Click(object sender, RoutedEventArgs e)
        {
            BtnBuild.IsEnabled = false;
            Log("=== Starting Build Process ===");

            // Auto Increment Version
            if (ChkAutoIncrement.IsChecked == true)
            {
                try
                {
                    Log(">>> Incrementing Project Version...");
                    string newVersion = UnityHelper.IncrementProjectVersion(_selectedProjectPath);
                    Log($"New Version: {newVersion}");
                }
                catch (Exception ex)
                {
                    Log($"Failed to increment version: {ex.Message}");
                }
            }

            // Determine build folder
            string buildFolder = !string.IsNullOrEmpty(_selectedOutputPath)
                ? _selectedOutputPath
                : System.IO.Path.Combine(_selectedProjectPath, "Builds");

            if (!System.IO.Directory.Exists(buildFolder))
                System.IO.Directory.CreateDirectory(buildFolder);

            Log($"Build Output Directory: {buildFolder}");

            // Collect Options
            bool isDev = ChkDevBuild.IsChecked == true;
            bool isDebug = ChkScriptDebug.IsChecked == true;
            bool isProfiler = ChkProfiler.IsChecked == true;
            bool isClean = ChkCleanBuild.IsChecked == true;

            var jobs = new List<BuildJob>();

            string exeName = TxtExeName.Text;
            if (string.IsNullOrWhiteSpace(exeName)) exeName = "Game";

            // Helper to create job
            BuildJob CreateJob(BuildPlatform p) => new BuildJob
            {
                Platform = p,
                ProjectPath = _selectedProjectPath,
                UnityEditorPath = _currentProjectInfo.EditorPath,
                OutputPath = buildFolder,
                DevelopmentBuild = isDev,
                ScriptDebugging = isDebug,
                AutoConnectProfiler = isProfiler,
                CleanBuild = isClean,
                ExeName = exeName
            };

            if (ChkWindows.IsChecked == true) jobs.Add(CreateJob(BuildPlatform.Windows64));
            if (ChkAndroid.IsChecked == true) jobs.Add(CreateJob(BuildPlatform.Android));
            if (ChkIOS.IsChecked == true) jobs.Add(CreateJob(BuildPlatform.iOS));
            if (ChkWebGL.IsChecked == true) jobs.Add(CreateJob(BuildPlatform.WebGL));
            if (ChkMacOS.IsChecked == true) jobs.Add(CreateJob(BuildPlatform.MacOS));
            if (ChkLinux.IsChecked == true) jobs.Add(CreateJob(BuildPlatform.Linux));

            // Start Timer and Progress
            _buildStartTime = DateTime.Now;
            _buildTimer.Start();
            BuildProgressBar.IsIndeterminate = true;

            try
            {
                await _buildManager.StartBuildQueue(jobs);

                // --- Post-Processing ---

                // 1. Auto Zip
                if (ChkZipOutput.IsChecked == true)
                {
                    Log(">>> Starting Archiving Process...");
                    foreach (var job in jobs)
                    {
                        // Helper to map BuildManager paths for zipping
                        string subFolder = "";
                        switch (job.Platform)
                        {
                            case BuildPlatform.Windows64: subFolder = "Windows"; break;
                            case BuildPlatform.Android: subFolder = "Android"; break;
                            case BuildPlatform.Linux: subFolder = "Linux"; break;
                            case BuildPlatform.iOS: subFolder = "iOS"; break;
                            case BuildPlatform.WebGL: subFolder = "WebGL"; break;
                            case BuildPlatform.MacOS: subFolder = "MacOS"; break;
                        }

                        string platformFolder = System.IO.Path.Combine(job.OutputPath, subFolder);

                        if (System.IO.Directory.Exists(platformFolder))
                        {
                            string zipPath = System.IO.Path.Combine(job.OutputPath, $"{exeName}_{job.Platform}_{DateTime.Now:yyyyMMdd_HHmm}.zip");
                            if (System.IO.File.Exists(zipPath)) System.IO.File.Delete(zipPath);

                            Log($"Archiving {job.Platform}...");
                            await Task.Run(() => System.IO.Compression.ZipFile.CreateFromDirectory(platformFolder, zipPath));
                            Log($"Created: {System.IO.Path.GetFileName(zipPath)}");
                        }
                    }
                }

                // 2. Sound Notification
                SystemSounds.Exclamation.Play();

                // 3. Open Folder
                if (ChkOpenFolder.IsChecked == true)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = buildFolder,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }

                Log("=== Build Pipeline Finished Successfully ===");
            }
            catch (Exception ex)
            {
                Log($"CRITICAL ERROR: {ex.Message}");
            }
            finally
            {
                // Stop Timer and Progress
                _buildTimer.Stop();
                BuildProgressBar.IsIndeterminate = false;
                BuildProgressBar.Value = 100;
                ValidateRequirements(); // Reset button state
            }
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                var lowerMsg = message.ToLower();
                var run = new Run($"[{DateTime.Now.ToShortTimeString()}] {message}");
                var paragraph = new Paragraph(run);
                paragraph.Margin = new Thickness(0);

                if (lowerMsg.Contains("error") || lowerMsg.Contains("exception") || lowerMsg.Contains("failed"))
                {
                    run.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 85, 85)); // Red
                    run.FontWeight = FontWeights.Bold;
                }
                else if (lowerMsg.Contains("warning"))
                {
                    run.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 184, 108)); // Orange
                }
                else if (lowerMsg.Contains("success") || lowerMsg.Contains("completed"))
                {
                    run.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(80, 250, 123)); // Green
                    run.FontWeight = FontWeights.Bold;
                }
                else
                {
                    run.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 220, 220)); // Default Gray
                }

                RtbLogs.Document.Blocks.Add(paragraph);
                RtbLogs.ScrollToEnd();
            });
        }
    }
}