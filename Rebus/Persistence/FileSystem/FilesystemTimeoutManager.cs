using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rebus.Logging;
using Rebus.Time;
using Rebus.Timeouts;
#pragma warning disable 1998

namespace Rebus.Persistence.FileSystem;

/// <summary>
/// Implementation of <see cref="ITimeoutManager"/> that stores timeouts in the filesystem
/// </summary>
public class FileSystemTimeoutManager : ITimeoutManager
{
    static readonly string TickFormat;

    readonly string _basePath;
    readonly IRebusTime _rebusTime;
    readonly string _lockFile;
    readonly ILog _log;

    static FileSystemTimeoutManager()
    {
        var digitsInMaxInt = int.MaxValue.ToString().Length;

        TickFormat = new string('0', digitsInMaxInt);
    }

    /// <summary>
    /// Creates the timeout manager, storing timeouts in the given <paramref name="basePath"/>
    /// </summary>
    public FileSystemTimeoutManager(string basePath, IRebusLoggerFactory rebusLoggerFactory, IRebusTime rebusTime)
    {
        _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
        _rebusTime = rebusTime ?? throw new ArgumentNullException(nameof(rebusTime));
        _lockFile = Path.Combine(basePath, "lock.txt");
        _log = rebusLoggerFactory.GetLogger<FileSystemTimeoutManager>();
    }

    /// <summary>
    /// Stores the message to be retrieved later
    /// </summary>
    public async Task Defer(DateTimeOffset approximateDueTime, Dictionary<string, string> headers, byte[] body)
    {
        using (new FileSystemExclusiveLock(_lockFile, _log))
        {
            var prefix = approximateDueTime.UtcDateTime.Ticks.ToString(TickFormat);
            var count = Directory.EnumerateFiles(_basePath, prefix + "*.json").Count();
            var fileName = Path.Combine(_basePath, $"{prefix}_{count}.json");

            File.WriteAllText(fileName, JsonConvert.SerializeObject(new Timeout
            {
                Headers = headers,
                Body = body
            }));
        }
    }

    /// <summary>
    /// Gets all messages that are due at this instant
    /// </summary>
    public async Task<DueMessagesResult> GetDueMessages()
    {
        var lockItem = new FileSystemExclusiveLock(_lockFile, _log);
        var prefix = _rebusTime.Now.UtcDateTime.Ticks.ToString(TickFormat);
        var enumerable = Directory.EnumerateFiles(_basePath, "*.json")
            .Where(x => string.CompareOrdinal(prefix, 0, Path.GetFileNameWithoutExtension(x), 0, TickFormat.Length) >= 0)
            .ToList();

        var items = enumerable
            .Select(f => new
            {
                Timeout = JsonConvert.DeserializeObject<Timeout>(File.ReadAllText(f)),
                File = f
            })
            .Select(a => new DueMessage(a.Timeout.Headers, a.Timeout.Body, async () =>
            {
                if (File.Exists(a.File))
                {
                    File.Delete(a.File);
                }
            }))
            .ToList();

        return new DueMessagesResult(items, async () => { lockItem.Dispose(); });
    }

    class Timeout
    {
        public Dictionary<string, string> Headers { get; set; }
        public byte[] Body { get; set; }
    }
}