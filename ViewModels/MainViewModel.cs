using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TeronEmailClient.Models;
using TeronEmailClient.Services;

namespace TeronEmailClient.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ConfigService _configService;
    private readonly AppSettings _settings;
    private readonly bool _isLoaded;

    public MainViewModel(AppSettings settings, ConfigService configService)
    {
        _settings = settings;
        _configService = configService;

        foreach (EmailAccount account in settings.Accounts)
        {
            Accounts.Add(new AccountViewModel(account));
        }

        RememberLastAccount = settings.RememberLastAccount;
        NotificationsEnabled = settings.NotificationsEnabled;
        Theme = settings.Theme;
        ThemeManager.Apply(Theme);

        if (settings.RememberLastAccount && settings.ActiveAccountId is { } activeId)
        {
            SelectedAccount = Accounts.FirstOrDefault(a => a.Id == activeId);
        }

        _isLoaded = true;
    }

    public ObservableCollection<AccountViewModel> Accounts { get; } = [];

    [ObservableProperty]
    private AccountViewModel? _selectedAccount;

    [ObservableProperty]
    private bool _rememberLastAccount;

    [ObservableProperty]
    private bool _notificationsEnabled;

    [ObservableProperty]
    private AppTheme _theme;

    public bool ShowWelcome => SelectedAccount is null;

    partial void OnSelectedAccountChanged(AccountViewModel? value)
    {
        OnPropertyChanged(nameof(ShowWelcome));
        if (_isLoaded)
        {
            _ = PersistAsync();
        }
    }

    partial void OnRememberLastAccountChanged(bool value)
    {
        if (_isLoaded)
        {
            _ = PersistAsync();
        }
    }

    partial void OnNotificationsEnabledChanged(bool value)
    {
        if (_isLoaded)
        {
            _ = PersistAsync();
        }
    }

    partial void OnThemeChanged(AppTheme value)
    {
        ThemeManager.Apply(value);
        if (_isLoaded)
        {
            _ = PersistAsync();
        }
    }

    public AccountViewModel AddAccount(string email, string displayName, ServiceType service, string url)
    {
        // Accounts are keyed by email, not display name/provider - re-adding one that's already
        // signed in just re-selects the existing entry instead of creating a duplicate. An empty
        // email means a Gmail/Outlook sign-in is still pending (MainWindow fills it in once the
        // account's WebView2 reads the real address back from the signed-in page), so there's
        // nothing meaningful to dedupe against yet - always create a new entry for those.
        AccountViewModel? existing = string.IsNullOrEmpty(email)
            ? null
            : Accounts.FirstOrDefault(a => string.Equals(a.Email, email, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            SelectedAccount = existing;
            return existing;
        }

        EmailAccount account = new()
        {
            Email = email,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? $"Signing in to {ServiceCatalog.Get(service).DisplayName}…" : displayName,
            Service = service,
            Url = url
        };

        AccountViewModel viewModel = new(account);
        Accounts.Add(viewModel);
        SelectedAccount = viewModel;
        return viewModel;
    }

    public void RemoveAccount(AccountViewModel account)
    {
        if (SelectedAccount == account)
        {
            SelectedAccount = Accounts.FirstOrDefault(a => a != account);
        }

        Accounts.Remove(account);
        _ = PersistAsync();
    }

    public Task PersistAsync()
    {
        _settings.Accounts = Accounts.Select(a => a.Account).ToList();
        _settings.ActiveAccountId = SelectedAccount?.Id;
        _settings.RememberLastAccount = RememberLastAccount;
        _settings.NotificationsEnabled = NotificationsEnabled;
        _settings.Theme = Theme;
        return ConfigService.SaveAsync(_settings);
    }
}
