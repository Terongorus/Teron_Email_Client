using System.Windows;
using TeronEmailClient.Models;

namespace TeronEmailClient.Services;

public static class ThemeManager
{
    public static void Apply(AppTheme theme)
    {
        ResourceDictionary colors = new()
        {
            Source = new Uri(
                theme == AppTheme.Dark ? "Themes/Colors.Dark.xaml" : "Themes/Colors.Light.xaml",
                UriKind.Relative)
        };

        var mergedDictionaries = Application.Current.Resources.MergedDictionaries;

        ResourceDictionary? existingColors = mergedDictionaries.FirstOrDefault(d =>
            d.Source is not null && d.Source.OriginalString.Contains("Colors.", StringComparison.Ordinal));

        if (existingColors is not null)
        {
            int index = mergedDictionaries.IndexOf(existingColors);
            mergedDictionaries[index] = colors;
        }
        else
        {
            mergedDictionaries.Insert(0, colors);
        }
    }
}
