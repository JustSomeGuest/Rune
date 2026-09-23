using System;
using System.Windows;
using System.Windows.Threading;

namespace Rune
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnLastWindowClose;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                LogCrash("UnobservedTaskException", args.Exception);
                args.SetObserved();
            };
            DispatcherUnhandledException += OnDispatcherUnhandledException;

            try
            {
                AssetManager.EnsureExtracted();
            }
            catch (Exception ex)
            {
                LogCrash("AssetExtract", ex);
                MessageBox.Show($"Failed to extract UI assets:\n{ex.Message}\n\nDetails saved to %TEMP%\\Rune_crash.log",
                    "Rune", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
                return;
            }

            try
            {
                if (e.Args.Length > 0 && e.Args[0].Equals("--setup", StringComparison.OrdinalIgnoreCase))
                {
                    string? presetPath = e.Args.Length > 1 ? e.Args[1] : null;
                    var installer = new InstallerWindow();
                    if (!string.IsNullOrWhiteSpace(presetPath))
                        installer.PathTextBox.Text = presetPath;
                    installer.Show();
                }
                else if (e.Args.Length > 0 && e.Args[0].Equals("--uninstall", StringComparison.OrdinalIgnoreCase))
                {
                    var result = MessageBox.Show(
                        "Uninstall Rune? This will remove shortcuts and program files.",
                        "Uninstall Rune", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                        Uninstall();
                    else
                        Shutdown(0);
                }
                else
                {
                    string settingsPath = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Rune", "settings.json");

                    bool firstRun = !System.IO.File.Exists(settingsPath);

                    if (firstRun)
                    {
                        var installer = new InstallerWindow();
                        installer.Show();
                    }
                    else
                    {
                        var mainWindow = new MainWindow();
                        mainWindow.Show();
                    }
                }
            }
            catch (Exception ex)
            {
                LogCrash("Startup", ex);
                MessageBox.Show($"Rune failed to start:\n{ex.Message}\n\nDetails saved to %TEMP%\\Rune_crash.log",
                    "Rune", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        internal static void LogCrash(string context, Exception ex)
        {
            try
            {
                string log = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(), "Rune_crash.log");
                System.IO.File.AppendAllText(log,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}: {ex}\n");
            }
            catch { }
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception
                ?? new Exception("Unknown fatal error: " + e.ExceptionObject);
            LogCrash("Fatal", ex);
            MessageBox.Show($"Rune crashed:\n{ex.Message}\n\nDetails saved to %TEMP%\\Rune_crash.log",
                "Rune", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private static void Uninstall()
        {
            try
            {
                string exePath = Environment.ProcessPath ?? "";
                string exeDir = System.IO.Path.GetDirectoryName(exePath) ?? "";

                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string lnk = System.IO.Path.Combine(desktop, "Rune.lnk");
                if (System.IO.File.Exists(lnk)) System.IO.File.Delete(lnk);

                string programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                string runeDir = System.IO.Path.Combine(programs, "Rune");
                if (System.IO.Directory.Exists(runeDir))
                    System.IO.Directory.Delete(runeDir, true);

                MessageBox.Show("Rune shortcuts removed. Please delete the program folder manually to complete uninstall.",
                    "Uninstall Rune", MessageBoxButton.OK, MessageBoxImage.Information);

                // Schedule self-delete on exit
                if (!string.IsNullOrEmpty(exeDir) && exeDir.Contains("Rune", StringComparison.OrdinalIgnoreCase))
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/C timeout /t 1 /nobreak >nul && rmdir /s /q \"{exeDir}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    System.Diagnostics.Process.Start(psi);
                }

                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Uninstall error: {ex.Message}", "Uninstall Rune",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogCrash("UI", e.Exception);
            MessageBox.Show(
                $"Rune hit an unexpected error and needs to recover:\n\n{e.Exception.Message}\n\nDetails saved to %TEMP%\\Rune_crash.log",
                "Rune",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            e.Handled = true;
        }
    }
}
