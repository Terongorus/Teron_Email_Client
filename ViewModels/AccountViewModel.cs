using CommunityToolkit.Mvvm.ComponentModel;
using TeronEmailClient.Models;

namespace TeronEmailClient.ViewModels;

public partial class AccountViewModel : ObservableObject
{
    public AccountViewModel(EmailAccount account)
    {
        Account = account;
    }

    public EmailAccount Account { get; }

    public Guid Id => Account.Id;
    public string Email => Account.Email;
    public string DisplayName => Account.DisplayName;
    public string Url => Account.Url;
    public ServiceType Service => Account.Service;
    public string ProfileFolder => Account.ProfileFolder;
    public string AccentColor => ServiceCatalog.Get(Account.Service).AccentColor;

    public string Initial =>
        string.IsNullOrWhiteSpace(DisplayName) ? "?" : char.ToUpperInvariant(DisplayName.Trim()[0]).ToString();

    // Gmail/Outlook accounts start with no known email/display name - MainWindow calls this once
    // it reads the real signed-in address back out of the account's own WebView2, after the user
    // finishes the provider's own OAuth flow.
    public void UpdateIdentity(string email, string displayName)
    {
        Account.Email = email;
        Account.DisplayName = displayName;
        OnPropertyChanged(nameof(Email));
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(Initial));
    }

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _canGoBack;

    [ObservableProperty]
    private bool _canGoForward;

    [ObservableProperty]
    private bool _hasBeenActivated;
}
