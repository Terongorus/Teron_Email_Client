using System.Windows;
using System.Windows.Controls;
using TeronEmailClient.Models;
using TeronEmailClient.ViewModels;

namespace TeronEmailClient.Views;

public partial class SettingsWindow : Window
{
    private readonly MainViewModel _viewModel;

    public SettingsWindow(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;

        InitializeComponent();

        DarkModeToggle.IsChecked = viewModel.Theme == AppTheme.Dark;
    }

    private void DarkModeToggle_Changed(object sender, RoutedEventArgs e)
    {
        _viewModel.Theme = DarkModeToggle.IsChecked == true ? AppTheme.Dark : AppTheme.Light;
    }

    private void RemoveAccountButton_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is not AccountViewModel account)
        {
            return;
        }

        MessageBoxResult result = MessageBox.Show(
            this,
            $"Remove \"{account.DisplayName}\"? This will sign you out and delete its local session data.",
            "Remove account",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            _viewModel.RemoveAccount(account);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
