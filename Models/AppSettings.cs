namespace TeronEmailClient.Models;

public sealed class AppSettings
{
    public List<EmailAccount> Accounts { get; set; } = [];
    public Guid? ActiveAccountId { get; set; }
    public bool RememberLastAccount { get; set; } = true;
    public bool NotificationsEnabled { get; set; } = true;
    public AppTheme Theme { get; set; } = AppTheme.Light;
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public double? WindowWidth { get; set; }
    public double? WindowHeight { get; set; }
    public string? SavedWindowState { get; set; }
}
