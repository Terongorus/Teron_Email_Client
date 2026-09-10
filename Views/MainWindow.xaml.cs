using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
    private readonly HashSet<Guid> _identityDetectionStarted = [];
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
        PreviewKeyDown += MainWindow_PreviewKeyDown;

        RestoreWindowStateFromSettings();

        Loaded += async (_, _) =>
        {
            // Config is already loaded once in App.OnStartup, before this window and its
            // MainViewModel are constructed. Reloading it here would hand back a second,
            // disconnected AppSettings instance: ConfigService.Current would repoint to it while
            // MainViewModel keeps mutating the original object, so any account added mid-session
            // gets silently discarded when OnWindowClosing later saves the (stale) Current.
            await ActivateAccountAsync(_viewModel.SelectedAccount);
        };
        Closing += MainWindow_Closing;
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

    private bool _closeConfirmed;

    // Window.Closing has no awaitable form - an `async void` subscriber returns to WPF at its
    // first `await`, so the window (and, via ShutdownMode=OnMainWindowClose, the whole process)
    // can tear down while the config save is still in flight. This was very likely the real
    // reason accounts never survived a restart: cancel the close, actually wait for the save,
    // then close for real (with the flag guarding against cancelling that second close too).
    private async void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_closeConfirmed)
        {
            return;
        }

        e.Cancel = true;
        await SaveSettingsOnClosingAsync();
        _closeConfirmed = true;
        Close();
    }

    private async Task SaveSettingsOnClosingAsync()
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

            _identityDetectionStarted.Remove(removed.Id);

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

        // WebView2's content runs in its own HWND, so by default Chromium's own accelerator table
        // silently consumes keys like F5/Home/Alt+Left/Alt+Right while the page (not the WPF
        // chrome) has focus - they never tunnel through the Window's PreviewKeyDown at all. Turning
        // this off is the documented way to make WebView2 forward them as ordinary routed WPF
        // key events instead, so MainWindow_PreviewKeyDown can handle them consistently either way.
        webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;

        // WebView2 already renders the web Notifications API as native Windows toasts by default
        // (same pipeline Edge uses) - the only reason mail providers' own "new message" desktop
        // notifications never appeared is that a chromeless embedded WebView2 has no permission-
        // prompt UI to grant the request through, so it was left stuck pending/denied. Granting it
        // ourselves (gated on the Settings toggle) is what actually turns them on; the provider's
        // own "desktop notifications" setting (e.g. Gmail's) still needs to be on too.
        webView.CoreWebView2.PermissionRequested += (_, args) =>
        {
            if (args.PermissionKind == CoreWebView2PermissionKind.Notifications)
            {
                args.State = _viewModel.NotificationsEnabled ? CoreWebView2PermissionState.Allow : CoreWebView2PermissionState.Deny;
            }
        };

        webView.CoreWebView2.NewWindowRequested += (_, args) => HandleNewWindowRequested(webView, args);
        webView.CoreWebView2.HistoryChanged += (_, _) =>
        {
            account.CanGoBack = webView.CoreWebView2.CanGoBack;
            account.CanGoForward = webView.CoreWebView2.CanGoForward;
        };
        webView.CoreWebView2.NavigationStarting += (_, _) => account.IsLoading = true;
        webView.CoreWebView2.NavigationCompleted += (_, _) =>
        {
            account.IsLoading = false;
            TryDetectPendingIdentity(webView, account);
        };

        account.HasBeenActivated = true;
        webView.CoreWebView2.Navigate(account.Url);

        return webView;
    }

    // Gmail/Outlook accounts are created with no known email - the user signs in for real on the
    // provider's own page inside the account's WebView2, so there's no form to read it back from.
    // Once that navigation lands back on the provider's actual mail app (not still on a
    // login/consent host), best-effort scrape the signed-in address out of the page itself.
    private static bool IsProviderAppHost(ServiceType service, string host) => service switch
    {
        ServiceType.Gmail => host.Equals("mail.google.com", StringComparison.OrdinalIgnoreCase),
        ServiceType.Outlook => host.Equals("outlook.office.com", StringComparison.OrdinalIgnoreCase)
            || host.Equals("outlook.office365.com", StringComparison.OrdinalIgnoreCase)
            || host.Equals("outlook.live.com", StringComparison.OrdinalIgnoreCase),
        _ => false
    };

    private const string DetectSignedInEmailScript = """
        (function () {
            const emailPattern = /[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}/;
            const selectors = [
                '[aria-label*="Google Account" i]',
                '[aria-label*="account" i]',
                '[title*="account" i]',
                '[aria-label*="profile" i]',
                '#O365_MainLink_Me',
                '#meControl'
            ];
            for (const selector of selectors) {
                for (const el of document.querySelectorAll(selector)) {
                    const text = (el.getAttribute('aria-label') || '') + ' ' + (el.getAttribute('title') || '');
                    const match = text.match(emailPattern);
                    if (match) {
                        return match[0];
                    }
                }
            }
            return null;
        })();
        """;

    private void TryDetectPendingIdentity(WebView2 webView, AccountViewModel account)
    {
        if (!string.IsNullOrEmpty(account.Email) || account.Service == ServiceType.Custom)
        {
            return;
        }

        if (!Uri.TryCreate(webView.CoreWebView2.Source, UriKind.Absolute, out Uri? uri) ||
            !IsProviderAppHost(account.Service, uri.Host))
        {
            return;
        }

        if (!_identityDetectionStarted.Add(account.Id))
        {
            return;
        }

        _ = DetectSignedInIdentityAsync(webView, account);
    }

    private async Task DetectSignedInIdentityAsync(WebView2 webView, AccountViewModel account)
    {
        // The account chrome (and therefore the profile-menu markup the script looks for) can take
        // a while to finish rendering after the top-level navigation itself completes, so this
        // polls for a bit instead of trusting a single pass right after NavigationCompleted.
        for (int attempt = 0; attempt < 10; attempt++)
        {
            await Task.Delay(1500);

            if (!string.IsNullOrEmpty(account.Email) || webView.CoreWebView2 is null)
            {
                return;
            }

            string raw;
            try
            {
                raw = await webView.CoreWebView2.ExecuteScriptAsync(DetectSignedInEmailScript);
            }
            catch (Exception)
            {
                return;
            }

            string? email = System.Text.Json.JsonSerializer.Deserialize<string?>(raw);
            if (string.IsNullOrWhiteSpace(email))
            {
                continue;
            }

            account.UpdateIdentity(email, email[..email.IndexOf('@')]);

            if (_viewModel.SelectedAccount == account)
            {
                Title = $"{account.DisplayName} - {AppInfo.DisplayNameWithVersion}";
                TitleBarText.Text = Title;
            }

            _ = _viewModel.PersistAsync();
            return;
        }
    }

    // OAuth/sign-in popups (e.g. Google's "Sign in" challenge from inside Gmail) must stay inside
    // an embedded WebView2 sharing the account's own profile - that's the only way the resulting
    // session cookies land in the same cookie jar the account's main WebView2 reads from. Anything
    // else opening via window.open()/target=_blank (a link inside an email, say) is ordinary web
    // content and belongs in the user's actual default browser, not a bare native popup window.
    private static readonly string[] TrustedAuthHosts =
    [
        "accounts.google.com",
        "login.microsoftonline.com",
        "login.live.com",
        "login.windows.net",
    ];

    private static bool IsTrustedAuthHost(string host) =>
        TrustedAuthHosts.Any(trusted =>
            host.Equals(trusted, StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith("." + trusted, StringComparison.OrdinalIgnoreCase));

    private void HandleNewWindowRequested(WebView2 owner, CoreWebView2NewWindowRequestedEventArgs e)
    {
        if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out Uri? uri) || !IsTrustedAuthHost(uri.Host))
        {
            e.Handled = true;
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri) { UseShellExecute = true });
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // No associated default browser/handler for this URI; nothing else to do.
            }

            return;
        }

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

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (TryHandleToolbarShortcut(e.Key, Keyboard.Modifiers))
        {
            e.Handled = true;
        }
    }

    private bool TryHandleToolbarShortcut(Key key, ModifierKeys modifiers)
    {
        switch (key)
        {
            case Key.F5:
            case Key.BrowserRefresh:
                ReloadButton_Click(this, new RoutedEventArgs());
                return true;
            case Key.Home:
            case Key.BrowserHome:
                HomeButton_Click(this, new RoutedEventArgs());
                return true;
            case Key.BrowserBack:
                BackButton_Click(this, new RoutedEventArgs());
                return true;
            case Key.BrowserForward:
                ForwardButton_Click(this, new RoutedEventArgs());
                return true;
            case Key.Left when modifiers == ModifierKeys.Alt:
                BackButton_Click(this, new RoutedEventArgs());
                return true;
            case Key.Right when modifiers == ModifierKeys.Alt:
                ForwardButton_Click(this, new RoutedEventArgs());
                return true;
            default:
                return false;
        }
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
            _viewModel.AddAccount(draft.Email, draft.DisplayName, draft.Service, draft.Url);
            _ = _viewModel.PersistAsync();
        }
    }

    private void WelcomeTile_Click(object sender, RoutedEventArgs e)
    {
        string tag = (string)((Button)sender).Tag;
        ServiceType? preselect = tag == "Custom" ? ServiceType.Custom : Enum.Parse<ServiceType>(tag);

        AddAccountWindow dialog = new(preselect) { Owner = this };
        if (dialog.ShowDialog() == true && dialog.Result is { } draft)
        {
            _viewModel.AddAccount(draft.Email, draft.DisplayName, draft.Service, draft.Url);
            _ = _viewModel.PersistAsync();
        }
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
