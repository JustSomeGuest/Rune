using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Web.WebView2.Core;

namespace Rune
{
    public partial class MainWindow : Window
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase)
        {
            ".git",
            "bin",
            "obj",
            "node_modules",
            ".vs",
            ".idea",
            "packages"
        };

        private const int DWMWA_BORDER_COLOR = 34;
        private const int WM_GETMINMAXINFO = 0x0024;
        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int HTCAPTION = 0x0002;
        private const int MONITOR_DEFAULTTONEAREST = 2;

        private string? workspacePath;
        private bool forceClose;
        private readonly string settingsPath;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            int attribute,
            ref int value,
            int valueSize);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(
            IntPtr hwnd,
            uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(
            IntPtr hMonitor,
            ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(
            IntPtr hWnd,
            int Msg,
            IntPtr wParam,
            IntPtr lParam);

        public MainWindow()
        {
            InitializeComponent();

            settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Rune",
                "settings.json");

            workspacePath = LoadLastWorkspace();

            SourceInitialized += OnSourceInitialized;
            StateChanged += (_, _) => PostWindowState();
            Closing += OnWindowClosing;

            InitializeWebView();
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;

            int color = 0x0F0C0B;

            DwmSetWindowAttribute(
                hwnd,
                DWMWA_BORDER_COLOR,
                ref color,
                sizeof(int));

            HwndSource? source = HwndSource.FromHwnd(hwnd);

            source?.AddHook(WindowProc);
        }

        private IntPtr WindowProc(
            IntPtr hwnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled)
        {
            if (msg == WM_GETMINMAXINFO)
            {
                WmGetMinMaxInfo(hwnd, lParam);
                handled = true;
            }

            return IntPtr.Zero;
        }

        private static void WmGetMinMaxInfo(
            IntPtr hwnd,
            IntPtr lParam)
        {
            MINMAXINFO mmi =
                Marshal.PtrToStructure<MINMAXINFO>(lParam);

            IntPtr monitor =
                MonitorFromWindow(
                    hwnd,
                    MONITOR_DEFAULTTONEAREST);

            if (monitor != IntPtr.Zero)
            {
                MONITORINFO monitorInfo = new()
                {
                    cbSize = Marshal.SizeOf<MONITORINFO>()
                };

                if (GetMonitorInfo(
                        monitor,
                        ref monitorInfo))
                {
                    RECT workArea = monitorInfo.rcWork;
                    RECT monitorArea = monitorInfo.rcMonitor;

                    mmi.ptMaxPosition.X =
                        workArea.Left -
                        monitorArea.Left;

                    mmi.ptMaxPosition.Y =
                        workArea.Top -
                        monitorArea.Top;

                    mmi.ptMaxSize.X =
                        workArea.Right -
                        workArea.Left;

                    mmi.ptMaxSize.Y =
                        workArea.Bottom -
                        workArea.Top;
                }
            }

            Marshal.StructureToPtr(
                mmi,
                lParam,
                true);
        }

        private async void InitializeWebView()
        {
            await WebView.EnsureCoreWebView2Async();

            WebView.CoreWebView2.Settings.IsNonClientRegionSupportEnabled = false;

            WebView.CoreWebView2.WebMessageReceived += WebMessageReceived;

            string uiPath = Path.Combine(
                AppContext.BaseDirectory,
                "UI",
                "index.html");

            WebView.CoreWebView2.Navigate(
                new Uri(uiPath).AbsoluteUri);
        }

        private void WebMessageReceived(
            object? sender,
            CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                using JsonDocument document =
                    JsonDocument.Parse(e.WebMessageAsJson);

                JsonElement message = document.RootElement;

                string type =
                    message.GetProperty("type")
                        .GetString() ?? "";

                switch (type)
                {
                    case "init":
                        SendWorkspace();
                        PostWindowState();
                        SendSettings();
                        break;

                    case "saveSettings":
                        SaveSettings(message);
                        break;

                    case "drag":
                        BeginWindowDrag();
                        break;

                    case "read":
                    case "openFile":
                        ReadFile(message);
                        break;

                    case "save":
                    case "saveFile":
                        SaveFile(message);
                        break;

                    case "saveAs":
                        SaveFileAs(message);
                        break;

                    case "openFileDialog":
                        OpenFilesFromDialog();
                        break;

                    case "openFolder":
                        OpenFolderFromDialog();
                        break;

                    case "newFile":
                        CreateEntry(message, false);
                        break;

                    case "newFolder":
                        CreateEntry(message, true);
                        break;

                    case "rename":
                        RenameEntry(message);
                        break;

                    case "delete":
                        DeleteEntry(message);
                        break;

                    case "reveal":
                        RevealInExplorer(message);
                        break;

                    case "refresh":
                        SendWorkspace();
                        break;

                    case "minimize":
                        WindowState = WindowState.Minimized;
                        break;

                    case "maximize":
                    case "maximizeToggle":
                        ToggleMaximize();
                        break;

                    case "close":
                    case "closeConfirmed":
                        forceClose = true;
                        Close();
                        break;
                }
            }
            catch (Exception ex)
            {
                SendError(ex.Message);
            }
        }

        private void BeginWindowDrag()
        {
            IntPtr hwnd =
                new WindowInteropHelper(this).Handle;

            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                PostWindowState();
            }

            ReleaseCapture();

            SendMessage(
                hwnd,
                WM_NCLBUTTONDOWN,
                new IntPtr(HTCAPTION),
                IntPtr.Zero);
        }

        private void ToggleMaximize()
        {
            WindowState =
                WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;

            PostWindowState();
        }

        private void PostWindowState()
        {
            Post(new
            {
                type = "windowState",
                maximized =
                    WindowState == WindowState.Maximized
            });
        }

        private void OnWindowClosing(
            object? sender,
            CancelEventArgs e)
        {
            if (forceClose)
            {
                return;
            }

            e.Cancel = true;

            Post(new
            {
                type = "confirmClose"
            });
        }

        private void SendWorkspace()
        {
            if (workspacePath == null ||
                !Directory.Exists(workspacePath))
            {
                Post(new
                {
                    type = "workspace",
                    root = (string?)null,
                    files = Array.Empty<string>(),
                    tree = Array.Empty<FileNode>()
                });

                return;
            }

            List<FileNode> tree =
                BuildTree(
                    workspacePath,
                    workspacePath);

            List<string> files =
                FlattenFiles(tree);

            Post(new
            {
                type = "workspace",
                root = Path.GetFileName(
                    workspacePath.TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar)),
                files,
                tree
            });
        }

        private List<string> FlattenFiles(
            IEnumerable<FileNode> nodes)
        {
            var result = new List<string>();

            foreach (FileNode node in nodes)
            {
                if (node.Type == "file")
                {
                    result.Add(node.Path);
                }

                if (node.Children != null)
                {
                    result.AddRange(
                        FlattenFiles(node.Children));
                }
            }

            return result;
        }

        private List<FileNode> BuildTree(
            string directory,
            string workspaceRoot)
        {
            var nodes = new List<FileNode>();

            IEnumerable<string> directories;
            IEnumerable<string> files;

            try
            {
                directories =
                    Directory.EnumerateDirectories(directory)
                        .OrderBy(
                            Path.GetFileName,
                            StringComparer.OrdinalIgnoreCase);

                files =
                    Directory.EnumerateFiles(directory)
                        .OrderBy(
                            Path.GetFileName,
                            StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return nodes;
            }

            foreach (string dir in directories)
            {
                string name =
                    Path.GetFileName(dir);

                if (ExcludedDirectories.Contains(name))
                {
                    continue;
                }

                string relativePath =
                    Path.GetRelativePath(
                        workspaceRoot,
                        dir)
                        .Replace('\\', '/');

                nodes.Add(
                    new FileNode(
                        name,
                        relativePath,
                        "folder",
                        BuildTree(
                            dir,
                            workspaceRoot)));
            }

            foreach (string file in files)
            {
                string relativePath =
                    Path.GetRelativePath(
                        workspaceRoot,
                        file)
                        .Replace('\\', '/');

                nodes.Add(
                    new FileNode(
                        Path.GetFileName(file),
                        relativePath,
                        "file",
                        null));
            }

            return nodes;
        }

        private static readonly HashSet<string> BinaryExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".zip", ".exe", ".dll", ".bin", ".7z", ".rar", ".iso",
            ".png", ".jpg", ".jpeg", ".gif", ".webp", ".ico",
            ".mp3", ".mp4", ".wav", ".ogg", ".pdf", ".tar", ".gz",
            ".class", ".obj", ".lib", ".apk", ".dmg", ".pkg", ".deb", ".rpm"
        };

        private static bool IsBinaryFile(string filePath)
        {
            try
            {
                string ext = Path.GetExtension(filePath);
                if (BinaryExtensions.Contains(ext))
                {
                    return true;
                }

                using var stream = File.OpenRead(filePath);
                byte[] buffer = new byte[1024];
                int read = stream.Read(buffer, 0, buffer.Length);
                for (int i = 0; i < read; i++)
                {
                    if (buffer[i] == 0)
                    {
                        return true;
                    }
                }
            }
            catch
            {
            }
            return false;
        }

        private void ReadFile(JsonElement message)
        {
            string relativePath =
                message.GetProperty("path")
                    .GetString() ?? "";

            try
            {
                string fullPath =
                    ResolvePath(relativePath);

                if (!File.Exists(fullPath))
                {
                    SendError(
                        "File not found.",
                        relativePath);

                    return;
                }

                if (IsBinaryFile(fullPath))
                {
                    SendError($"Binary file '{Path.GetFileName(fullPath)}' cannot be opened as source code.", relativePath);
                    return;
                }

                string content =
                    File.ReadAllText(fullPath);

                Post(new
                {
                    type = "file",
                    path = relativePath,
                    content
                });
            }
            catch (Exception ex)
            {
                SendError(
                    ex.Message,
                    relativePath);
            }
        }

        private void SaveFile(JsonElement message)
        {
            string relativePath =
                message.GetProperty("path")
                    .GetString() ?? "";

            string content =
                message.GetProperty("content")
                    .GetString() ?? "";

            try
            {
                string fullPath =
                    ResolvePath(relativePath);

                string? directory =
                    Path.GetDirectoryName(fullPath);

                if (directory != null)
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(
                    fullPath,
                    content);

                Post(new
                {
                    type = "saveResult",
                    success = true,
                    path = relativePath
                });
            }
            catch (Exception ex)
            {
                SendError(
                    ex.Message,
                    relativePath);

                Post(new
                {
                    type = "saveResult",
                    success = false,
                    path = relativePath
                });
            }
        }

        private void SaveFileAs(
            JsonElement message)
        {
            string currentPath =
                message.TryGetProperty(
                    "path",
                    out JsonElement p)
                    ? p.GetString() ?? ""
                    : "";

            string content =
                message.GetProperty("content")
                    .GetString() ?? "";

            var dialog =
                new Microsoft.Win32.SaveFileDialog
                {
                    FileName =
                        string.IsNullOrEmpty(currentPath)
                            ? "Untitled.txt"
                            : Path.GetFileName(currentPath),

                    InitialDirectory =
                        ResolveInitialDirectory(currentPath),

                    Filter =
                        "All Files (*.*)|*.*"
                };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            try
            {
                File.WriteAllText(
                    dialog.FileName,
                    content);

                string newPath =
                    ToWorkspaceRelativeOrAbsolute(
                        dialog.FileName);

                Post(new
                {
                    type = "savedAs",
                    oldPath = currentPath,
                    newPath,
                    content
                });

                if (IsInsideWorkspace(dialog.FileName))
                {
                    SendWorkspace();
                }
            }
            catch (Exception ex)
            {
                SendError(ex.Message);
            }
        }

        private void OpenFilesFromDialog()
        {
            var dialog =
                new Microsoft.Win32.OpenFileDialog
                {
                    InitialDirectory =
                        workspacePath ??
                        Environment.GetFolderPath(
                            Environment.SpecialFolder.MyDocuments),

                    Filter =
                        "All Files (*.*)|*.*",

                    Multiselect = true
                };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            foreach (string file in dialog.FileNames)
            {
                try
                {
                    if (IsBinaryFile(file))
                    {
                        SendError($"Binary file '{Path.GetFileName(file)}' cannot be opened as source code.", file);
                        continue;
                    }

                    string content =
                        File.ReadAllText(file);

                    string path =
                        ToWorkspaceRelativeOrAbsolute(file);

                    Post(new
                    {
                        type = "file",
                        path,
                        content
                    });
                }
                catch (Exception ex)
                {
                    SendError(
                        ex.Message,
                        file);
                }
            }
        }

        private void OpenFolderFromDialog()
        {
            var dialog =
                new Microsoft.Win32.OpenFolderDialog
                {
                    InitialDirectory =
                        workspacePath ??
                        Environment.GetFolderPath(
                            Environment.SpecialFolder.Desktop),

                    Title = "Open Folder"
                };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            workspacePath =
                Path.GetFullPath(dialog.FolderName);

            SaveLastWorkspace(workspacePath);

            SendWorkspace();
        }

        private void CreateEntry(
            JsonElement message,
            bool isFolder)
        {
            string parent =
                message.TryGetProperty(
                    "parentPath",
                    out JsonElement pp)
                    ? pp.GetString() ?? ""
                    : "";

            string name =
                message.GetProperty("name")
                    .GetString() ?? "";

            if (string.IsNullOrWhiteSpace(name))
            {
                SendError("Name cannot be empty.");
                return;
            }

            try
            {
                if (workspacePath == null)
                {
                    SendError("Open a folder first.");
                    return;
                }

                string parentFull =
                    string.IsNullOrEmpty(parent)
                        ? workspacePath
                        : ResolvePath(parent);

                string fullPath =
                    Path.Combine(
                        parentFull,
                        name);

                if (File.Exists(fullPath) ||
                    Directory.Exists(fullPath))
                {
                    SendError(
                        "A file or folder with that name already exists.");

                    return;
                }

                if (isFolder)
                {
                    Directory.CreateDirectory(fullPath);
                }
                else
                {
                    File.WriteAllText(fullPath, "");
                }

                string relativePath =
                    Path.GetRelativePath(
                        workspacePath,
                        fullPath)
                        .Replace('\\', '/');

                SendWorkspace();

                Post(new
                {
                    type = "created",
                    path = relativePath,
                    kind = isFolder
                        ? "folder"
                        : "file"
                });
            }
            catch (Exception ex)
            {
                SendError(ex.Message);
            }
        }

        private void RenameEntry(
            JsonElement message)
        {
            string path =
                message.GetProperty("path")
                    .GetString() ?? "";

            string newName =
                message.GetProperty("newName")
                    .GetString() ?? "";

            if (string.IsNullOrWhiteSpace(newName))
            {
                SendError("Name cannot be empty.");
                return;
            }

            try
            {
                if (workspacePath == null)
                {
                    SendError("Open a folder first.");
                    return;
                }

                string fullPath =
                    ResolvePath(path);

                bool isDirectory =
                    Directory.Exists(fullPath);

                if (!isDirectory &&
                    !File.Exists(fullPath))
                {
                    SendError(
                        "File not found.",
                        path);

                    return;
                }

                string? parent =
                    Path.GetDirectoryName(fullPath);

                if (parent == null)
                {
                    SendError("Cannot rename this item.");
                    return;
                }

                string newFullPath =
                    Path.Combine(
                        parent,
                        newName);

                if (File.Exists(newFullPath) ||
                    Directory.Exists(newFullPath))
                {
                    SendError(
                        "A file or folder with that name already exists.");

                    return;
                }

                if (isDirectory)
                {
                    Directory.Move(
                        fullPath,
                        newFullPath);
                }
                else
                {
                    File.Move(
                        fullPath,
                        newFullPath);
                }

                string newRelativePath =
                    Path.GetRelativePath(
                        workspacePath,
                        newFullPath)
                        .Replace('\\', '/');

                SendWorkspace();

                Post(new
                {
                    type = "renamed",
                    oldPath = path,
                    newPath = newRelativePath,
                    kind = isDirectory
                        ? "folder"
                        : "file"
                });
            }
            catch (Exception ex)
            {
                SendError(
                    ex.Message,
                    path);
            }
        }

        private void DeleteEntry(
            JsonElement message)
        {
            string path =
                message.GetProperty("path")
                    .GetString() ?? "";

            try
            {
                string fullPath =
                    ResolvePath(path);

                if (Directory.Exists(fullPath))
                {
                    Directory.Delete(
                        fullPath,
                        true);
                }
                else if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
                else
                {
                    SendError(
                        "File not found.",
                        path);

                    return;
                }

                SendWorkspace();

                Post(new
                {
                    type = "deleted",
                    path
                });
            }
            catch (Exception ex)
            {
                SendError(
                    ex.Message,
                    path);
            }
        }

        private void RevealInExplorer(
            JsonElement message)
        {
            string path =
                message.GetProperty("path")
                    .GetString() ?? "";

            try
            {
                string fullPath =
                    ResolvePath(path);

                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments =
                            Directory.Exists(fullPath)
                                ? $"\"{fullPath}\""
                                : $"/select,\"{fullPath}\"",
                        UseShellExecute = true
                    });
            }
            catch (Exception ex)
            {
                SendError(
                    ex.Message,
                    path);
            }
        }

        private string ResolvePath(
            string requestedPath)
        {
            if (Path.IsPathRooted(requestedPath))
            {
                return Path.GetFullPath(requestedPath);
            }

            if (workspacePath == null)
            {
                throw new InvalidOperationException(
                    "No folder is open.");
            }

            string fullPath =
                Path.GetFullPath(
                    Path.Combine(
                        workspacePath,
                        requestedPath));

            string root =
                Path.GetFullPath(workspacePath)
                    .TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            if (!fullPath.StartsWith(
                    root,
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(
                    fullPath,
                    Path.GetFullPath(workspacePath),
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException(
                    "That path is outside the open folder.");
            }

            return fullPath;
        }

        private bool IsInsideWorkspace(
            string fullPath)
        {
            if (workspacePath == null)
            {
                return false;
            }

            string root =
                Path.GetFullPath(workspacePath)
                    .TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            return Path.GetFullPath(fullPath)
                .StartsWith(
                    root,
                    StringComparison.OrdinalIgnoreCase);
        }

        private string ToWorkspaceRelativeOrAbsolute(
            string fullPath)
        {
            return IsInsideWorkspace(fullPath)
                ? Path.GetRelativePath(
                    workspacePath!,
                    fullPath)
                    .Replace('\\', '/')
                : fullPath;
        }

        private string ResolveInitialDirectory(
            string currentPath)
        {
            if (!string.IsNullOrEmpty(currentPath))
            {
                try
                {
                    string full =
                        ResolvePath(currentPath);

                    string? dir =
                        Path.GetDirectoryName(full);

                    if (dir != null &&
                        Directory.Exists(dir))
                    {
                        return dir;
                    }
                }
                catch
                {
                }
            }

            return workspacePath ??
                Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments);
        }

        private string? LoadLastWorkspace()
        {
            try
            {
                if (!File.Exists(settingsPath))
                {
                    return null;
                }

                SettingsData? data =
                    JsonSerializer.Deserialize<SettingsData>(
                        File.ReadAllText(settingsPath),
                        JsonOptions);

                return data?.LastWorkspace is
                    { Length: > 0 } path &&
                    Directory.Exists(path)
                        ? path
                        : null;
            }
            catch
            {
                return null;
            }
        }

        private void SaveLastWorkspace(
            string? path)
        {
            try
            {
                string? directory =
                    Path.GetDirectoryName(settingsPath);

                if (directory != null)
                {
                    Directory.CreateDirectory(directory);
                }

                SettingsData? data = null;
                if (File.Exists(settingsPath))
                {
                    data = JsonSerializer.Deserialize<SettingsData>(
                        File.ReadAllText(settingsPath),
                        JsonOptions);
                }

                data ??= new SettingsData();
                data.LastWorkspace = path;

                File.WriteAllText(
                    settingsPath,
                    JsonSerializer.Serialize(
                        data,
                        JsonOptions));
            }
            catch
            {
            }
        }

        private void SendSettings()
        {
            try
            {
                if (!File.Exists(settingsPath)) return;
                string json = File.ReadAllText(settingsPath);
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                string? theme = root.TryGetProperty("theme", out var t) ? t.GetString() : "dark";
                string? uiStyle = root.TryGetProperty("uiStyle", out var u) ? u.GetString() : "modern";
                string? accentColor = root.TryGetProperty("accentColor", out var a) ? a.GetString() : "#8b5cf6";

                Post(new
                {
                    type = "settings",
                    theme,
                    uiStyle,
                    accentColor
                });
            }
            catch { }
        }

        private void SaveSettings(JsonElement message)
        {
            try
            {
                string? directory = Path.GetDirectoryName(settingsPath);
                if (directory != null) Directory.CreateDirectory(directory);

                string theme = message.TryGetProperty("theme", out var t) ? t.GetString() ?? "dark" : "dark";
                string uiStyle = message.TryGetProperty("uiStyle", out var u) ? u.GetString() ?? "modern" : "modern";
                string accentColor = message.TryGetProperty("accentColor", out var a) ? a.GetString() ?? "#8b5cf6" : "#8b5cf6";

                SettingsData? data = null;
                if (File.Exists(settingsPath))
                {
                    data = JsonSerializer.Deserialize<SettingsData>(File.ReadAllText(settingsPath), JsonOptions);
                }
                data ??= new SettingsData();
                data.Theme = theme;
                data.UiStyle = uiStyle;
                data.AccentColor = accentColor;

                File.WriteAllText(settingsPath, JsonSerializer.Serialize(data, JsonOptions));
            }
            catch (Exception ex)
            {
                SendError(ex.Message);
            }
        }

        private void Post(object message)
        {
            if (WebView.CoreWebView2 == null)
            {
                return;
            }

            WebView.CoreWebView2.PostWebMessageAsJson(
                JsonSerializer.Serialize(
                    message,
                    JsonOptions));
        }

        private void SendError(
            string messageText,
            string? path = null)
        {
            Post(new
            {
                type = "error",
                message = messageText,
                path
            });
        }

        private record FileNode(
            string Name,
            string Path,
            string Type,
            List<FileNode>? Children);

        private class SettingsData
        {
            [JsonPropertyName("lastWorkspace")]
            public string? LastWorkspace { get; set; }

            [JsonPropertyName("theme")]
            public string? Theme { get; set; }

            [JsonPropertyName("uiStyle")]
            public string? UiStyle { get; set; }

            [JsonPropertyName("accentColor")]
            public string? AccentColor { get; set; }
        }
    }
}
