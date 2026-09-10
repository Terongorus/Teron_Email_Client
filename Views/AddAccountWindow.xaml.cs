using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using TeronEmailClient.Models;

namespace TeronEmailClient.Views;

public partial class AddAccountWindow : Window
{
    private readonly List<ToggleButton> _tiles;
    private ServiceDefinition? _selectedService;

    public AddAccountWindow(ServiceType? preselect = null)
    {
        InitializeComponent();

        _tiles = [GmailTile, OutlookTile, CustomTile];

        if (preselect is { } type)
        {
            ToggleButton? match = _tiles.FirstOrDefault(t => (string)t.Tag == type.ToString());
            if (match is not null)
            {
                match.IsChecked = true;
            }
        }
    }

    public EmailAccountDraft? Result { get; private set; }

    private void ServiceTile_Checked(object sender, RoutedEventArgs e)
    {
        ToggleButton clicked = (ToggleButton)sender;

        foreach (ToggleButton tile in _tiles.Where(t => t != clicked))
        {
            tile.IsChecked = false;
        }

        ServiceType service = Enum.Parse<ServiceType>((string)clicked.Tag);
        _selectedService = ServiceCatalog.Get(service);

        // Gmail/Outlook sign-in happens for real once the account's own WebView2 navigates to the
        // provider - that page asks for the email/password itself (actual OAuth), so asking for an
        // email address up front here was just a redundant, easy-to-get-wrong copy of that. The
        // manual email/display name/URL fields are only meaningful for a mailbox we can't drive an
        // OAuth flow against at all: a custom/self-hosted webmail URL.
        bool isCustom = service == ServiceType.Custom;

        UrlLabel.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
        UrlBox.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
        UrlBox.Text = isCustom ? string.Empty : _selectedService.DefaultUrl;

        EmailLabel.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
        EmailBox.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
        DisplayNameLabel.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
        DisplayNameBox.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;

        OAuthHintText.Visibility = isCustom ? Visibility.Collapsed : Visibility.Visible;
        OAuthHintText.Text = $"You'll sign in to {_selectedService.DisplayName} directly on the next screen - your email and name are read from that sign-in, not typed here.";

        AddButton.Content = isCustom ? "Add account" : $"Continue to {_selectedService.DisplayName}";

        UpdateAddButtonEnabled();
    }

    private void EmailBox_TextChanged(object sender, TextChangedEventArgs e) => UpdateAddButtonEnabled();

    private void UpdateAddButtonEnabled()
    {
        if (_selectedService is null)
        {
            AddButton.IsEnabled = false;
            return;
        }

        AddButton.IsEnabled = _selectedService.Type != ServiceType.Custom || IsValidEmail(EmailBox.Text);
    }

    private static bool IsValidEmail(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        try
        {
            _ = new MailAddress(text.Trim());
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedService is null)
        {
            return;
        }

        if (_selectedService.Type != ServiceType.Custom)
        {
            // Email/display name are unknown until the caller drives the actual OAuth sign-in in
            // the account's own WebView2 - MainWindow fills both in afterward once it reads them
            // back from the signed-in page.
            Result = new EmailAccountDraft(string.Empty, string.Empty, _selectedService.Type, _selectedService.DefaultUrl);
            DialogResult = true;
            return;
        }

        string email = EmailBox.Text.Trim();
        if (!IsValidEmail(email))
        {
            MessageBox.Show(this, "Please enter a valid email address - this is what identifies the account.", "Invalid email",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Fall back to the email's local part, never the provider name - "Gmail"/"Outlook" as a
        // display name is what made every account's sidebar badge collapse to the same "G"/"O".
        string displayName = string.IsNullOrWhiteSpace(DisplayNameBox.Text) ? email[..email.IndexOf('@')] : DisplayNameBox.Text.Trim();
        string url = UrlBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            MessageBox.Show(this, "Please enter a valid URL (including https://).", "Invalid URL",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result = new EmailAccountDraft(email, displayName, ServiceType.Custom, url);
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void CloseButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
