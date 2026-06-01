using System.Text.Json.Serialization;
using UotanInstaller.App.Models;

namespace UotanInstaller.App.Services;

[JsonSerializable(typeof(GitHubRelease))]
[JsonSerializable(typeof(GitHubReleaseAsset))]
[JsonSerializable(typeof(GitHubRelease[]))]
[JsonSerializable(typeof(ReleaseChannelInfo))]
[JsonSerializable(typeof(ComponentDefinition))]
[JsonSerializable(typeof(ComponentDefinition[]))]
[JsonSerializable(typeof(VersionRecord))]
[JsonSerializable(typeof(VersionRecord[]))]
[JsonSerializable(typeof(DeltaUpdateInfo))]
internal sealed partial class AppJsonContext : JsonSerializerContext
{
}