# Unity Queued Builder

**Unity Queued Builder** is a standalone WPF application designed to streamline, standardize, and automate the build process for Unity developers. Manage your build queues, handle versioning, and export to multiple platforms simultaneously through a clean, modern, external interface without battling with CLI arguments or Editor windows.

## 🚀 Key Features

*   **⚡ Multi-Platform Batching:** Queue builds for Windows (x64), Android, iOS, WebGL, macOS, and Linux in a single operation.
*   **📂 Smart Project Detection:** Automatically integrates with Unity Hub history (Registry & JSON) to populate your recent projects list.
*   **💾 Build Profiles:** Save and load your most frequent build configurations (e.g., "Nightly Debug", "Production Release").
*   **🔄 Workflow Automation:**
    *   **Auto Versioning:** Automatically increment version numbers (patch level) before building.
    *   **Archiving:** Option to automatically ZIP build outputs.
    *   **Maintenance:** One-click option to clean the build cache.
*   **📝 Live Activity Log:** Real-time, consolidated logs from the Unity build pipeline displayed directly in the UI.
*   **🎨 Modern Design:** A dark-themed, minimalist, and "flat" UI designed for professional workflows.

## 🛠 Usage

1.  **Select Project:** Choose a project from the dropdown (synced with Unity Hub) or browse manually.
2.  **Define Target:** Set your custom **Executable Name** and choose the **Output Path**.
3.  **Select Platforms:** Check the boxes for all the platforms you wish to build for.
4.  **Configure:** Toggle options like *Development Build*, *Script Debugging*, or *Auto Increment*.
5.  **Build:** Click **BUILD**. The tool will launch Unity in batch mode and process the queue.

## 📦 Requirements

*   **OS:** Windows 10/11
*   **Unity:** Unity Editor installed (compatible with the target project version)
*   **.NET:** .NET 6.0 or higher (if running from source)

## 🤝 Contributing

Contributions, issues, and feature requests are welcome!

## 📄 License

This project is open source.


## How to Run
1. Open the project in Visual Studio or VS Code.
2. Run `dotnet run` in the terminal.
3. Select your Unity Project folder.
4. Select Target Platforms.
5. Click **BUILD ALL**.

## Output
Builds will be placed in a `Builds` folder inside your Unity Project directory.
- Windows: `Builds/Windows/Game.exe`
- Android: `Builds/Android/Game.apk`
