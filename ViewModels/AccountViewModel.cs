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
    public string DisplayName => Account.DisplayName;
    public string Url => Account.Url;
    public ServiceType Service => Account.Service;
    public string ProfileFolder => Account.ProfileFolder;
    public string AccentColor => ServiceCatalog.Get(Account.Service).AccentColor;

    public string Initial =>
        string.IsNullOrWhiteSpace(DisplayName) ? "?" : char.ToUpperInvariant(DisplayName.Trim()[0]).ToString();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _canGoBack;

    [ObservableProperty]
    private bool _canGoForward;

    [ObservableProperty]
    private bool _hasBeenActivated;
}
