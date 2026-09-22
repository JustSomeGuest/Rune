using System;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Windows;

namespace Rune
{
    public partial class InstallerWindow : Window
    {
        private int currentStep = 1;

        public InstallerWindow()
        {
            InitializeComponent();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            if (currentStep == 1)
            {
                string targetDir = PathTextBox.Text.Trim();
                if (string.IsNullOrEmpty(targetDir))
                {
                    MessageBox.Show("Please specify a valid installation path.", "Rune Setup", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                currentStep = 2;
                Step1Panel.Visibility = Visibility.Collapsed;
                Step2Panel.Visibility = Visibility.Visible;
                CancelBtn.IsEnabled = false;
                NextBtn.IsEnabled = false;

                try
                {
                    await System.Threading.Tasks.Task.Run(() => PerformInstallation(targetDir));

                    currentStep = 3;
                    Step2Panel.Visibility = Visibility.Collapsed;
                    Step3Panel.Visibility = Visibility.Visible;
                    CancelBtn.Visibility = Visibility.Collapsed;
                    NextBtn.Content = "Launch Rune";
                    NextBtn.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Installation failed:\n\n{ex.Message}", "Rune Setup", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                }
            }
            else if (currentStep == 3)
            {
                // Launch installed application or close setup
                string targetDir = PathTextBox.Text.Trim();
                string exePath = Path.Combine(targetDir, "Rune.exe");
                if (File.Exists(exePath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = true
                    });
                }
                Close();
            }
        }

        private void PerformInstallation(string targetDir)
        {
            // Create directory
            Directory.CreateDirectory(targetDir);

            // Copy files from AppContext.BaseDirectory
            string sourceDir = AppContext.BaseDirectory;
            foreach (string file in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(sourceDir, file);
                // Skip installer itself if bundled
                if (relativePath.Contains("installer", StringComparison.OrdinalIgnoreCase)) continue;

                string destFile = Path.Combine(targetDir, relativePath);
                string? destParent = Path.GetDirectoryName(destFile);
                if (destParent != null) Directory.CreateDirectory(destParent);

                File.Copy(file, destFile, true);
            }

            // Create settings.json
            string settingsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Rune");
            Directory.CreateDirectory(settingsDir);
            string settingsPath = Path.Combine(settingsDir, "settings.json");
            if (!File.Exists(settingsPath))
            {
                File.WriteAllText(settingsPath, "{\"lastWorkspace\":null,\"theme\":\"dark\",\"uiStyle\":\"modern\",\"accentColor\":\"#7c3aed\"}");
            }

            string exePath = Path.Combine(targetDir, "Rune.exe");

            // Desktop shortcut
            bool createDesktop = false;
            bool createStartMenu = false;
            Dispatcher.Invoke(() =>
            {
                createDesktop = DesktopShortcutCheck.IsChecked == true;
                createStartMenu = StartMenuCheck.IsChecked == true;
            });

            if (createDesktop)
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string shortcutPath = Path.Combine(desktopPath, "Rune.lnk");
                CreateShortcut(shortcutPath, exePath, "Rune Code Editor");
            }

            // Start Menu shortcut
            if (createStartMenu)
            {
                string startMenuPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                    "Rune");
                Directory.CreateDirectory(startMenuPath);
                string shortcutPath = Path.Combine(startMenuPath, "Rune.lnk");
                CreateShortcut(shortcutPath, exePath, "Rune Code Editor");
            }
        }

        private static void CreateShortcut(string shortcutPath, string targetPath, string description)
        {
            try
            {
                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    object? shell = Activator.CreateInstance(shellType);
                    if (shell != null)
                    {
                        var shortcut = shellType.InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath });
                        if (shortcut != null)
                        {
                            var shortcutType = shortcut.GetType();
                            shortcutType.InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { targetPath });
                            shortcutType.InvokeMember("Description", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { description });
                            shortcutType.InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { Path.GetDirectoryName(targetPath) ?? "" });
                            shortcutType.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, shortcut, null);
                        }
                    }
                }
            }
            catch
            {
            }
        }
    }
}
