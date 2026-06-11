#nullable enable

namespace Screenbox.Core.Models;

public sealed class JellyfinSyncProgress
{
    public JellyfinSyncProgress(string message, int processedCount = 0, int totalCount = 0)
    {
        Message = message;
        ProcessedCount = processedCount;
        TotalCount = totalCount;
    }

    public string Message { get; }

    public int ProcessedCount { get; }

    public int TotalCount { get; }
}
