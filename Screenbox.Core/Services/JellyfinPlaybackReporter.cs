#nullable enable

using System;
using CommunityToolkit.WinUI;
using Screenbox.Core.Events;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using Windows.System;
using Windows.Media.Playback;

namespace Screenbox.Core.Services;

public sealed class JellyfinPlaybackReporter : IJellyfinPlaybackReporter
{
    private readonly IJellyfinService _jellyfinService;
    private readonly DispatcherQueueTimer _progressTimer;
    private IMediaPlayer? _player;
    private JellyfinMediaSource? _currentSource;

    public JellyfinPlaybackReporter(IJellyfinService jellyfinService)
    {
        _jellyfinService = jellyfinService;
        _progressTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _progressTimer.Interval = TimeSpan.FromSeconds(10);
        _progressTimer.Tick += OnProgressTimerTick;
    }

    public void Attach(IMediaPlayer player)
    {
        Detach(player);
        _player = player;
        player.PlaybackItemChanged += OnPlaybackItemChanged;
        player.PlaybackStateChanged += OnPlaybackStateChanged;
        player.PositionChanged += OnPositionChanged;
    }

    public void Detach(IMediaPlayer player)
    {
        player.PlaybackItemChanged -= OnPlaybackItemChanged;
        player.PlaybackStateChanged -= OnPlaybackStateChanged;
        player.PositionChanged -= OnPositionChanged;
        if (_player == player) _player = null;
        _progressTimer.Stop();
    }

    private async void OnPlaybackItemChanged(IMediaPlayer sender, ValueChangedEventArgs<PlaybackItem?> args)
    {
        if (_currentSource != null) await _jellyfinService.ReportPlaybackStoppedAsync(_currentSource, sender.Position.Ticks);
        _currentSource = args.NewValue?.OriginalSource as JellyfinMediaSource;
        if (_currentSource != null) await _jellyfinService.ReportPlaybackStartAsync(_currentSource, sender.Position.Ticks);
    }

    private async void OnPlaybackStateChanged(IMediaPlayer sender, ValueChangedEventArgs<MediaPlaybackState> args)
    {
        if (_currentSource == null) return;
        if (args.NewValue == MediaPlaybackState.Playing)
        {
            _progressTimer.Start();
            await _jellyfinService.ReportPlaybackProgressAsync(_currentSource, sender.Position.Ticks, false);
        }
        else if (args.NewValue == MediaPlaybackState.Paused)
        {
            await _jellyfinService.ReportPlaybackProgressAsync(_currentSource, sender.Position.Ticks, true);
        }
        else if (args.NewValue == MediaPlaybackState.None)
        {
            _progressTimer.Stop();
            await _jellyfinService.ReportPlaybackStoppedAsync(_currentSource, sender.Position.Ticks);
        }
    }

    private void OnPositionChanged(IMediaPlayer sender, ValueChangedEventArgs<TimeSpan> args)
    {
    }

    private async void OnProgressTimerTick(object? sender, object e)
    {
        if (_player == null || _currentSource == null) return;
        await _jellyfinService.ReportPlaybackProgressAsync(_currentSource, _player.Position.Ticks, _player.PlaybackState == MediaPlaybackState.Paused);
    }
}
