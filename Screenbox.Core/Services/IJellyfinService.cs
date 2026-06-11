#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Screenbox.Core.Models;

namespace Screenbox.Core.Services;

public interface IJellyfinService
{
    bool IsConnected { get; }
    string LastSyncStatus { get; set; }
    JellyfinConnection GetConnection();
    Task<bool> AuthenticateAsync(string serverUrl, string username, string password, CancellationToken cancellationToken = default);
    void Disconnect();
    Task<MusicLibrary> FetchMusicAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default);
    Task<VideosLibrary> FetchVideosAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default);
    Task ReportPlaybackStartAsync(JellyfinMediaSource source, long positionTicks, CancellationToken cancellationToken = default);
    Task ReportPlaybackProgressAsync(JellyfinMediaSource source, long positionTicks, bool isPaused, CancellationToken cancellationToken = default);
    Task ReportPlaybackStoppedAsync(JellyfinMediaSource source, long positionTicks, CancellationToken cancellationToken = default);
}
