namespace TeronEmailClient.Models;

public sealed class EmailAccount
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string DisplayName { get; set; }
    public required ServiceType Service { get; set; }
    public required string Url { get; set; }
    public string ProfileFolder { get; init; } = Guid.NewGuid().ToString("N");
}
