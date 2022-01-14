using System;

namespace Rebus.Transport.FileSystem;

/// <summary>
/// File system-based transport options
/// </summary>
public class FileSystemTransportOptions
{
    /// <summary>
    /// Configures how many files to "prefetch", i.e. acquire file locks on
    /// </summary>
    public FileSystemTransportOptions Prefetch(int prefecthCount)
    {
        if (prefecthCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(prefecthCount), prefecthCount, "Please use a positive number as the prefetch count");
        }
        PrefetchCount = prefecthCount;
        return this;
    }

    internal int PrefetchCount { get; set; } = 10;
}