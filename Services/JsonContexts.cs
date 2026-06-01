using System.Text.Json.Serialization;
using UotanInstaller.App.Models;

namespace UotanInstaller.App.Services;

[JsonSerializable(typeof(GitHubRelease))]
[JsonSerializable(typeof(GitHubReleaseAsset))]
[JsonSerializable(typeof(GitHubRelease[]))]
[JsonSerializable(typeof(ReleaseChannelInfo))]
internal sealed partial class AppJsonContext : JsonSerializerContext
{
}