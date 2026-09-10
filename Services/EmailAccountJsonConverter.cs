using System.Text.Json;
using System.Text.Json.Serialization;
using TeronEmailClient.Models;

namespace TeronEmailClient.Services;

/// <summary>
/// Reads config.json files saved before accounts had an Email field, so upgrading doesn't throw
/// on the missing required member and silently reset the whole settings file (window position,
/// theme, every other account) back to defaults. Missing Email deserializes to "" - callers treat
/// that as "needs re-linking" rather than crashing.
/// </summary>
internal sealed class EmailAccountJsonConverter : JsonConverter<EmailAccount>
{
    public override EmailAccount Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;

        return new EmailAccount
        {
            Id = root.TryGetProperty(nameof(EmailAccount.Id), out JsonElement idProp) && idProp.TryGetGuid(out Guid id)
                ? id
                : Guid.NewGuid(),
            Email = root.TryGetProperty(nameof(EmailAccount.Email), out JsonElement emailProp) ? emailProp.GetString() ?? "" : "",
            DisplayName = root.TryGetProperty(nameof(EmailAccount.DisplayName), out JsonElement nameProp) ? nameProp.GetString() ?? "" : "",
            Service = root.TryGetProperty(nameof(EmailAccount.Service), out JsonElement serviceProp)
                ? (ServiceType)serviceProp.GetInt32()
                : ServiceType.Custom,
            Url = root.TryGetProperty(nameof(EmailAccount.Url), out JsonElement urlProp) ? urlProp.GetString() ?? "" : "",
            ProfileFolder = root.TryGetProperty(nameof(EmailAccount.ProfileFolder), out JsonElement folderProp) && folderProp.GetString() is { } folder
                ? folder
                : Guid.NewGuid().ToString("N"),
        };
    }

    public override void Write(Utf8JsonWriter writer, EmailAccount value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(nameof(EmailAccount.Id), value.Id);
        writer.WriteString(nameof(EmailAccount.Email), value.Email);
        writer.WriteString(nameof(EmailAccount.DisplayName), value.DisplayName);
        writer.WriteNumber(nameof(EmailAccount.Service), (int)value.Service);
        writer.WriteString(nameof(EmailAccount.Url), value.Url);
        writer.WriteString(nameof(EmailAccount.ProfileFolder), value.ProfileFolder);
        writer.WriteEndObject();
    }
}
