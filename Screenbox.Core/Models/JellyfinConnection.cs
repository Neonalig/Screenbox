#nullable enable

namespace Screenbox.Core.Models;

public sealed class JellyfinConnection
{
    public JellyfinConnection(string serverUrl, string accessToken, string userId, string deviceId)
    {
        ServerUrl = serverUrl;
        AccessToken = accessToken;
        UserId = userId;
        DeviceId = deviceId;
    }

    public string ServerUrl { get; }

    public string AccessToken { get; }

    public string UserId { get; }

    public string DeviceId { get; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ServerUrl)
        && !string.IsNullOrWhiteSpace(AccessToken)
        && !string.IsNullOrWhiteSpace(UserId);
}
