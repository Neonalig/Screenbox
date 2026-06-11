#nullable enable

using System;
using Screenbox.Core.Enums;

namespace Screenbox.Core.Models;

public sealed class JellyfinMediaSource
{
    public JellyfinMediaSource(string itemId, string serverUrl, string accessToken, string name, MediaPlaybackType mediaType, TimeSpan duration, string? libraryId = null)
    {
        ItemId = itemId;
        ServerUrl = serverUrl.TrimEnd('/');
        AccessToken = accessToken;
        Name = name;
        MediaType = mediaType;
        Duration = duration;
        LibraryId = libraryId ?? string.Empty;
    }

    public string ItemId { get; }

    public string ServerUrl { get; }

    public string AccessToken { get; }

    public string Name { get; }

    public MediaPlaybackType MediaType { get; }

    public TimeSpan Duration { get; }

    public string LibraryId { get; }
    public string Location => $"jellyfin://{ItemId}";
    public Uri StreamUri => new Uri($"{ServerUrl}/Videos/{Uri.EscapeDataString(ItemId)}/stream?static=true&api_key={Uri.EscapeDataString(AccessToken)}");
}
