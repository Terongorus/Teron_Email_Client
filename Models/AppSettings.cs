namespace TeronEmailClient.Models;

public sealed class AppSettings
{
    public List<EmailAccount> Accounts { get; set; } = [];
    public Guid? ActiveAccountId { get; set; }
    public bool RememberLastAccount { get; set; } = true;
    public AppTheme Theme { get; set; } = AppTheme.Light;
}
