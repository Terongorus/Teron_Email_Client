using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using TeronEmailClient.Models;
using TeronEmailClient.Services;
using TeronEmailClient.ViewModels;

namespace TeronEmailClient.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly ConfigService _settings;
    private readonly Dictionary<Guid, WebView2> _webViews = [];
    private WebView2? _activeWebView;

    public MainWindow(MainViewModel viewModel, ConfigService configService)
    {
        _viewModel = viewModel;
        _settings = configService;
        DataContext = viewModel;

        InitializeComponent();
        WindowChromeHelper.FixMaximizedBounds(this);
        Title = AppInfo.DisplayNameWithVersion;
        TitleBarText.Text = Title;

        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        viewModel.Accounts.CollectionChanged += OnAccountsCollectionChanged;

        RestoreWindowStateFromSettings();

        Loaded += async (_, _) =>
        {
            await _settings.LoadAsync();
            await ActivateAccountAsync(_viewModel.SelectedAccount);
        };
        Closing += async (sender, e) =>
        {
            await OnWindowClosing(sender, e);
        };
        Closed += (_, _) =>
        {
            foreach (WebView2 webView in _webViews.Values)
            {
                webView.Dispose();
            }
        };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        NativeMethods.TryEnableRoundedCorners(new WindowInteropHelper(this).Handle);
    }

    private async Task OnWindowClosing(object? sender, CancelEventArgs e)
    {
        AppSettings settings = _settings.Current;
        Rect bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
        if (bounds.Width > 0 && bounds.Height > 0)
        {
            settings.WindowLeft = bounds.Left;
            settings.WindowTop = bounds.Top;
            settings.WindowWidth = bounds.Width;
            settings.WindowHeight = bounds.Height;
        }

        settings.SavedWindowState = WindowState.ToString();
        bool saveResult = await ConfigService.SaveAsync(settings);
        if (!saveResult)
        {
            throw new InvalidOperationException("Failed to save configuration.");
        }
    }

    private static bool IsOnVirtualScreen(double left, double top, double width, double height)
    {
        double screenLeft = SystemParameters.VirtualScreenLeft;
        double screenTop = SystemParameters.VirtualScreenTop;
        double screenRight = screenLeft + SystemParameters.VirtualScreenWidth;
        double screenBottom = screenTop + SystemParameters.VirtualScreenHeight;
        return left < screenRight && left + width > screenLeft && top < screenBottom && top + height > screenTop;
    }

    private void RestoreWindowStateFromSettings()
    {
        AppSettings s = _settings.Current;

        if (s.WindowWidth is double w && s.WindowHeight is double h && w > 0 && h > 0)
        {
            Width = w;
            Height = h;
        }

        // Only trust a saved position if the window would still land on a currently-connected
        // monitor -- otherwise a since-removed second monitor could strand it off-screen forever.
        if (s.WindowLeft is double l && s.WindowTop is double t && IsOnVirtualScreen(l, t, Width, Height))
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = l;
            Top = t;
        }

        if (Enum.TryParse(s.SavedWindowState, out WindowState savedState))
        {
            WindowState = savedState;
        }
    }

    private async void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedAccount))
        {
            await ActivateAccountAsync(_viewModel.SelectedAccount);
        }
    }

    private void OnAccountsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is null)
        {
            return;
        }

        foreach (AccountViewModel removed in e.OldItems.Cast<AccountViewModel>())
        {
            if (!_webViews.Remove(removed.Id, out WebView2? webView))
            {
                continue;
            }

            // Hide it now, but defer destroying the native browser control to a Background-priority
            // callback so WPF gets a chance to actually repaint the "now hidden" state first. Tearing
            // the control down in the same tick as the visibility/selection change leaves a stale
            // frame of its last rendered page ghosted on screen (a WebView2/airspace quirk).
            webView.Visibility = Visibility.Collapsed;

            if (_activeWebView == webView)
            {
                _activeWebView = null;
            }

            Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
            {
                WebViewHost.Children.Remove(webView);
                string? profileDirectory = webView.CreationProperties?.UserDataFolder;
                webView.Dispose();

                TryDeleteProfileDirectory(profileDirectory);
            });
        }
    }

    private static void TryDeleteProfileDirectory(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return;
        }

        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (IOException)
        {
            // WebView2 may still be releasing file handles right after Dispose(); not worth retrying.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private async Task ActivateAccountAsync(AccountViewModel? account)
    {
        foreach (WebView2 existing in _webViews.Values)
        {
            existing.Visibility = Visibility.Collapsed;
        }

        // Altap Salamander-style title: "<context> - <AppName> v<Version>", with the active
        // account standing in for "context" so the visible title bar and the taskbar/Alt-Tab
        // title always show the same string.
        Title = account is null ? AppInfo.DisplayNameWithVersion : $"{account.DisplayName} - {AppInfo.DisplayNameWithVersion}";
        TitleBarText.Text = Title;

        if (account is null)
        {
            _activeWebView = null;
            return;
        }

        if (!_webViews.TryGetValue(account.Id, out WebView2? webView))
        {
            webView = await CreateWebViewAsync(account);
        }

        webView.Visibility = Visibility.Visible;
        _activeWebView = webView;
    }

    private async Task<WebView2> CreateWebViewAsync(AccountViewModel account)
    {
        WebView2 webView = new()
        {
            Visibility = Visibility.Collapsed,
            DefaultBackgroundColor = System.Drawing.Color.White,
            CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = Path.Combine(ConfigService.ProfilesRootDirectory, account.ProfileFolder)
            }
        };

        WebViewHost.Children.Add(webView);
        _webViews[account.Id] = webView;

        await webView.EnsureCoreWebView2Async();

        webView.CoreWebView2.Settings.AreDevToolsEnabled = System.Diagnostics.Debugger.IsAttached;
        webView.CoreWebView2.NewWindowRequested += (_, args) => HandleNewWindowRequested(webView, args);
        webView.CoreWebView2.HistoryChanged += (_, _) =>
        {
            account.CanGoBack = webView.CoreWebView2.CanGoBack;
            account.CanGoForward = webView.CoreWebView2.CanGoForward;
        };
        webView.CoreWebView2.NavigationStarting += (_, _) => account.IsLoading = true;
        webView.CoreWebView2.NavigationCompleted += (_, _) => account.IsLoading = false;

        account.HasBeenActivated = true;
        webView.CoreWebView2.Navigate(account.Url);

        return webView;
    }

    private void HandleNewWindowRequested(WebView2 owner, CoreWebView2NewWindowRequestedEventArgs e)
    {
        CoreWebView2Deferral deferral = e.GetDeferral();

        Window popup = new()
        {
            Owner = this,
            Width = 520,
            Height = 680,
            Title = "Sign in",
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = (Brush)FindResource("ContentBackgroundBrush")
        };

        WebView2 popupView = new()
        {
            CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = owner.CreationProperties?.UserDataFolder
            }
        };

        popup.Content = popupView;

        popupView.CoreWebView2InitializationCompleted += (_, args) =>
        {
            if (args.IsSuccess)
            {
                e.NewWindow = popupView.CoreWebView2;
                popupView.CoreWebView2.WindowCloseRequested += (_, _) => popup.Close();
            }

            deferral.Complete();
        };

        popup.Closed += (_, _) => popupView.Dispose();

        _ = popupView.EnsureCoreWebView2Async();
        popup.Show();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e) => _activeWebView?.CoreWebView2?.GoBack();

    private void ForwardButton_Click(object sender, RoutedEventArgs e) => _activeWebView?.CoreWebView2?.GoForward();

    private void ReloadButton_Click(object sender, RoutedEventArgs e) => _activeWebView?.CoreWebView2?.Reload();

    private void HomeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedAccount is { } account)
        {
            _activeWebView?.CoreWebView2?.Navigate(account.Url);
        }
    }

    private void AddAccountButton_Click(object sender, RoutedEventArgs e)
    {
        AddAccountWindow dialog = new() { Owner = this };
        if (dialog.ShowDialog() == true && dialog.Result is { } draft)
        {
            _viewModel.AddAccount(draft.DisplayName, draft.Service, draft.Url);
            _ = _viewModel.PersistAsync();
        }
    }

    private void WelcomeTile_Click(object sender, RoutedEventArgs e)
    {
        string tag = (string)((Button)sender).Tag;

        if (tag == "Custom")
        {
            AddAccountWindow dialog = new(ServiceType.Custom) { Owner = this };
            if (dialog.ShowDialog() == true && dialog.Result is { } draft)
            {
                _viewModel.AddAccount(draft.DisplayName, draft.Service, draft.Url);
                _ = _viewModel.PersistAsync();
            }

            return;
        }

        ServiceType service = Enum.Parse<ServiceType>(tag);
        ServiceDefinition definition = ServiceCatalog.Get(service);
        _viewModel.AddAccount(definition.DisplayName, service, definition.DefaultUrl);
        _ = _viewModel.PersistAsync();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsWindow dialog = new(_viewModel) { Owner = this };
        dialog.ShowDialog();
    }

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        MaximizeRestorePath.Data = (Geometry)FindResource(
            WindowState == WindowState.Maximized ? "IconRestore" : "IconMaximize");

    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
