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

        DisplayNameBox.Text = _selectedService.DisplayName;

        bool isCustom = service == ServiceType.Custom;
        UrlLabel.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
        UrlBox.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
        UrlBox.Text = isCustom ? string.Empty : _selectedService.DefaultUrl;

        AddButton.IsEnabled = true;
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedService is null)
        {
            return;
        }

        string displayName = string.IsNullOrWhiteSpace(DisplayNameBox.Text) ? _selectedService.DisplayName : DisplayNameBox.Text.Trim();
        string url = _selectedService.Type == ServiceType.Custom ? UrlBox.Text.Trim() : _selectedService.DefaultUrl;

        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            MessageBox.Show(this, "Please enter a valid URL (including https://).", "Invalid URL",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result = new EmailAccountDraft(displayName, _selectedService.Type, url);
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void CloseButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
