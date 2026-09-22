using System.Windows;
using System.Windows.Threading;

namespace Rune
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += OnDispatcherUnhandledException;

            if (e.Args.Length > 0 && e.Args[0].Equals("--setup", System.StringComparison.OrdinalIgnoreCase))
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

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                $"Rune hit an unexpected error and needs to recover:\n\n{e.Exception.Message}",
                "Rune",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            e.Handled = true;
        }
    }
}
