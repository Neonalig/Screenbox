#nullable enable

using Screenbox.Core.Playback;

namespace Screenbox.Core.Services;

public interface IJellyfinPlaybackReporter
{
    void Attach(IMediaPlayer player);
    void Detach(IMediaPlayer player);
}
