using System.IO;
using System.Text.Json;
using TeronEmailClient.Models;

namespace TeronEmailClient.Services;

public sealed class ConfigService
{
    private static readonly string RootDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TeronEmailClient");

    private static readonly string ConfigFilePath = Path.Combine(RootDirectory, "config.json");

    public static string ProfilesRootDirectory => Path.Combine(RootDirectory, "Profiles");

    public async Task<AppSettings> LoadAsync()
    {
        try
        {
            if (!File.Exists(ConfigFilePath))
            {
                return new AppSettings();
            }

            await using FileStream stream = File.OpenRead(ConfigFilePath);
            AppSettings? settings = await JsonSerializer.DeserializeAsync(stream, AppSettingsJsonContext.Default.AppSettings);
            return settings ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public async Task SaveAsync(AppSettings settings)
    {
        Directory.CreateDirectory(RootDirectory);
        await using FileStream stream = File.Create(ConfigFilePath);
        await JsonSerializer.SerializeAsync(stream, settings, AppSettingsJsonContext.Default.AppSettings);
    }
}
