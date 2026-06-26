using System.Text.Json.Serialization;
using TeronEmailClient.Models;

namespace TeronEmailClient.Services;

[JsonSerializable(typeof(AppSettings))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class AppSettingsJsonContext : JsonSerializerContext
{
}
