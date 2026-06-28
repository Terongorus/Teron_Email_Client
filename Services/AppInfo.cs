using System.Reflection;

namespace TeronEmailClient.Services;

/// <summary>
/// Reads the app's display name and version from the assembly's metadata (set in
/// TeronEmailClient.csproj) instead of duplicating them as separate hardcoded literals, so
/// window titles and dialogs can't drift out of sync with the project file.
/// </summary>
internal static class AppInfo
{
    public static string DisplayName { get; } = GetDisplayName();

    /// <summary>Display name with version, e.g. "Teron Email Client v2.1.1" - the standard
    /// main window title format shared across this user's other Teron* apps.</summary>
    public static string DisplayNameWithVersion { get; } = $"{DisplayName} v{GetVersion()}";

    private static string GetDisplayName()
    {
        string product = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>()?.Product
            ?? "TeronEmailClient";
        int parenIndex = product.IndexOf(" (", StringComparison.Ordinal);
        return parenIndex > 0 ? product[..parenIndex] : product;
    }

    private static string GetVersion()
    {
        // AssemblyInformationalVersion is sourced directly from <Version> in the .csproj, kept
        // in sync with CHANGELOG.md - unlike AssemblyVersion, which the CLR always pads to four
        // numeric parts regardless of what's written in the project file.
        return Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? "0.0.0";
    }
}
