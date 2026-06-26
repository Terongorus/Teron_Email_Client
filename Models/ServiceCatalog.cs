namespace TeronEmailClient.Models;

public sealed record ServiceDefinition(ServiceType Type, string DisplayName, string DefaultUrl, string AccentColor);

public static class ServiceCatalog
{
    public static readonly ServiceDefinition Gmail = new(
        ServiceType.Gmail, "Gmail", "https://mail.google.com", "#EA4335");

    public static readonly ServiceDefinition Outlook = new(
        ServiceType.Outlook, "Outlook", "https://outlook.office.com/mail/", "#0078D4");

    public static readonly ServiceDefinition Custom = new(
        ServiceType.Custom, "Custom", string.Empty, "#6B5CE0");

    public static readonly IReadOnlyList<ServiceDefinition> KnownServices = [Gmail, Outlook];

    public static ServiceDefinition Get(ServiceType type) => type switch
    {
        ServiceType.Gmail => Gmail,
        ServiceType.Outlook => Outlook,
        _ => Custom
    };
}
