using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using TeronEmailClient.Models;
using TeronEmailClient.Services;
using TeronEmailClient.ViewModels;
using TeronEmailClient.Views;

namespace TeronEmailClient;

public partial class App : Application
{
    private Mutex? _singleInstanceMutex;
    private bool _ownsSingleInstanceMutex;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) => LogException(args.Exception);
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            LogException(args.Exception);
            args.SetObserved();
        };

        // Opening an already-existing named mutex never grants this thread ownership, even with
        // initiallyOwned: true - only the process that actually creates it owns it. Releasing it
        // unconditionally from the "already running" (second-instance) branch below was calling
        // ReleaseMutex on a handle this process never owned, throwing on every duplicate launch.
        _singleInstanceMutex = new Mutex(true, "TeronEmailClient.SingleInstance", out bool createdNew);
        _ownsSingleInstanceMutex = createdNew;
        if (!createdNew)
        {
            MessageBox.Show(
                $"{AppInfo.DisplayName} is already running.",
                AppInfo.DisplayName,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

        ConfigService configService = new();
        AppSettings settings = await configService.LoadAsync();

        MainViewModel viewModel = new(settings, configService);
        MainWindow mainWindow = new(viewModel, configService);

        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_ownsSingleInstanceMutex)
        {
            _singleInstanceMutex?.ReleaseMutex();
        }

        base.OnExit(e);
    }

    private static void LogException(Exception ex)
    {
        try
        {
            string logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TeronEmailClient",
                "error.log");

            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath, $"{DateTime.Now:O}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // Logging is best-effort; nothing else we can do if it fails.
        }
    }
}
