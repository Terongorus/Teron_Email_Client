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

    partial void OnThemeChanged(AppTheme value)
    {
        ThemeManager.Apply(value);
        if (_isLoaded)
        {
            _ = PersistAsync();
        }
    }

    public AccountViewModel AddAccount(string displayName, ServiceType service, string url)
    {
        EmailAccount account = new()
        {
            DisplayName = displayName,
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
        Accounts.Remove(account);
        if (SelectedAccount == account)
        {
            SelectedAccount = Accounts.FirstOrDefault();
        }

        _ = PersistAsync();
    }

    public Task PersistAsync()
    {
        _settings.Accounts = Accounts.Select(a => a.Account).ToList();
        _settings.ActiveAccountId = SelectedAccount?.Id;
        _settings.RememberLastAccount = RememberLastAccount;
        _settings.Theme = Theme;
        return _configService.SaveAsync(_settings);
    }
}
