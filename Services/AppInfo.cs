using System.Reflection;

namespace TeronEmailClient.Services;

/// <summary>
/// Reads the app's display name from the assembly's &lt;Product&gt; metadata (set in
/// TeronEmailClient.csproj) instead of duplicating it as separate hardcoded literals, so
/// window titles and dialogs can't drift out of sync with the project file.
/// </summary>
internal static class AppInfo
{
    public static string DisplayName { get; } = GetDisplayName();

    private static string GetDisplayName()
    {
        string product = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>()?.Product
            ?? "TeronEmailClient";
        int parenIndex = product.IndexOf(" (", StringComparison.Ordinal);
        return parenIndex > 0 ? product[..parenIndex] : product;
    }
}
