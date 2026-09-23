using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Rune
{
    public partial class InstallerWindow : Window
    {
        private int currentStep = 1;

        public InstallerWindow()
        {
            InitializeComponent();
            string defaultDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs", "Rune");
            PathTextBox.Text = defaultDir;
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Titlebar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                return;
            DragMove();
        }

        private void BrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select Installation Folder",
                InitialDirectory = PathTextBox.Text
            };
            if (dialog.ShowDialog() == true)
                PathTextBox.Text = dialog.FolderName;
        }

        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            if (currentStep == 1)
            {
                string targetDir = PathTextBox.Text.Trim();
                if (string.IsNullOrEmpty(targetDir))
                {
                    MessageBox.Show("Please specify a valid installation path.", "Rune Setup",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (targetDir.StartsWith(@"C:\Program Files", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBoxResult result = MessageBox.Show(
                        "Installing to Program Files requires administrator privileges. Restart as administrator?",
                        "Rune Setup", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        RestartElevated();
                        return;
                    }
                }

                BeginInstall(targetDir);
            }
            else if (currentStep == 3)
            {
                string targetDir = PathTextBox.Text.Trim();
                string exePath = Path.Combine(targetDir, "Rune.exe");
                if (File.Exists(exePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = true
                    });
                }
                Close();
            }
        }

        private void RestartElevated()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Environment.ProcessPath ?? "Rune.exe",
                    Arguments = $"--setup \"{PathTextBox.Text.Trim()}\"",
                    UseShellExecute = true,
                    Verb = "runas"
                });
                Close();
            }
            catch
            {
                // User declined UAC
            }
        }

        private async void BeginInstall(string targetDir)
        {
            currentStep = 2;
            Step1Panel.Visibility = Visibility.Collapsed;
            Step2Panel.Visibility = Visibility.Visible;
            CancelBtn.IsEnabled = false;
            NextBtn.IsEnabled = false;

            bool createDesktop = DesktopShortcutCheck.IsChecked == true;
            bool createStartMenu = StartMenuCheck.IsChecked == true;

            try
            {
                string report = await System.Threading.Tasks.Task.Run(() =>
                {
                    Dispatcher.Invoke(() => StatusText.Text = "Copying application files...");
                    return PerformInstallation(targetDir, createDesktop, createStartMenu);
                });

                currentStep = 3;
                Step2Panel.Visibility = Visibility.Collapsed;
                Step3Panel.Visibility = Visibility.Visible;
                CancelBtn.Visibility = Visibility.Collapsed;
                NextBtn.Content = "Launch Rune";
                NextBtn.IsEnabled = true;
                ReportText.Text = report;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Installation failed:\n\n{ex.Message}", "Rune Setup",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private string PerformInstallation(string targetDir, bool createDesktop, bool createStartMenu)
        {
            Directory.CreateDirectory(targetDir);

            string actualExe = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "Rune.exe");
            string targetExe = Path.Combine(targetDir, "Rune.exe");

            if (!string.Equals(
                Path.GetFullPath(actualExe),
                Path.GetFullPath(targetExe),
                StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(actualExe, targetExe, true);
            }

            AssetManager.EnsureExtracted();

            string settingsDir = AssetManager.AppDataDir;
            Directory.CreateDirectory(settingsDir);
            string settingsPath = Path.Combine(settingsDir, "settings.json");
            if (!File.Exists(settingsPath))
            {
                File.WriteAllText(settingsPath,
                    "{\"lastWorkspace\":null,\"theme\":\"dark\",\"uiStyle\":\"modern\",\"accentColor\":\"#7c3aed\"}");
            }

            AssetManager.MarkInstalled();

            if (createDesktop)
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                CreateShortcut(Path.Combine(desktop, "Rune.lnk"), targetExe, "Rune Code Editor");
            }

            if (createStartMenu)
            {
                string programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                string runeDir = Path.Combine(programs, "Rune");
                Directory.CreateDirectory(runeDir);
                CreateShortcut(Path.Combine(runeDir, "Rune.lnk"), targetExe, "Rune Code Editor");
                CreateShortcut(Path.Combine(runeDir, "Uninstall.lnk"), targetExe, "Uninstall Rune",
                    "--uninstall");
            }

            string[] requiredAssets =
            {
                AssetManager.IndexHtmlPath,
                Path.Combine(AssetManager.UiDir, "script.js"),
                Path.Combine(AssetManager.UiDir, "style.css"),
                Path.Combine(AssetManager.AssetsDir, "rune.svg")
            };

            int verified = 0;
            var missing = new System.Collections.Generic.List<string>();
            foreach (string f in requiredAssets)
            {
                if (File.Exists(f))
                    verified++;
                else
                    missing.Add(Path.GetFileName(f));
            }

            string report = $"Installed Rune.exe to {targetDir}\n";
            report += $"Assets: {AssetManager.UiDir}\n";
            report += $"Verified {verified}/{requiredAssets.Length} critical files";
            if (missing.Count > 0)
                report += $"\nMissing: {string.Join(", ", missing)}";
            else
                report += " - all OK";

            return report;
        }

        private static void CreateShortcut(string shortcutPath, string targetPath,
            string description, string arguments = "")
        {
            try
            {
                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return;

                object? shell = Activator.CreateInstance(shellType);
                if (shell == null) return;

                object? shortcut = shellType.InvokeMember("CreateShortcut",
                    System.Reflection.BindingFlags.InvokeMethod, null, shell,
                    new object[] { shortcutPath });

                if (shortcut == null) return;

                Type st = shortcut.GetType();
                st.InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty,
                    null, shortcut, new object[] { targetPath });
                st.InvokeMember("Description", System.Reflection.BindingFlags.SetProperty,
                    null, shortcut, new object[] { description });
                st.InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.SetProperty,
                    null, shortcut, new object[] { Path.GetDirectoryName(targetPath) ?? "" });
                if (!string.IsNullOrEmpty(arguments))
                    st.InvokeMember("Arguments", System.Reflection.BindingFlags.SetProperty,
                        null, shortcut, new object[] { arguments });
                st.InvokeMember("IconLocation", System.Reflection.BindingFlags.SetProperty,
                    null, shortcut, new object[] { targetPath + ",0" });
                st.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod,
                    null, shortcut, null);
            }
            catch { }
        }
    }
}
