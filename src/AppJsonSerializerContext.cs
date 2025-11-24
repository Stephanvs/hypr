using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hyprwt.Services;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(AppState))]
[JsonSerializable(typeof(VersionCheckService.GitHubRelease))]
[JsonSerializable(typeof(VersionInfo))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}