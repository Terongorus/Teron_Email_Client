using System.Configuration;
using System.IO;
using System.Text.Json;
using System.Windows.Navigation;
using TeronEmailClient.Models;

namespace TeronEmailClient.Services;

public sealed class ConfigService
{
    private static readonly string RootDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TeronEmailClient");

    private static readonly string ConfigFilePath = Path.Combine(RootDirectory, "config.json");

    public static string ProfilesRootDirectory => Path.Combine(RootDirectory, "Profiles");

    public AppSettings Current { get; private set; } = new();

    public async Task<AppSettings> LoadAsync()
    {
        try
        {
            if (!File.Exists(ConfigFilePath))
            {
                Current = new AppSettings();
                return Current;
            }

            await using FileStream stream = File.OpenRead(ConfigFilePath);
            AppSettings? settings = await JsonSerializer.DeserializeAsync(stream, AppSettingsJsonContext.Default.AppSettings);
            if (settings != null)
            {
                Current = settings;
            }
        }
        catch
        {
            Current = new AppSettings();
        }

        return Current;
    }

    public static async Task<bool> SaveAsync(AppSettings settings)
    {
        Directory.CreateDirectory(RootDirectory);
        await using FileStream stream = File.Create(ConfigFilePath);
        await JsonSerializer.SerializeAsync(stream, settings, AppSettingsJsonContext.Default.AppSettings);
        if (!File.Exists(ConfigFilePath))
        {
            return false;
        }
        return true;
    }
}
