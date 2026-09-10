namespace TeronEmailClient.Models;

public sealed record EmailAccountDraft(string Email, string DisplayName, ServiceType Service, string Url);
