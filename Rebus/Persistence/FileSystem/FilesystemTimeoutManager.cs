﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rebus.Logging;
using Rebus.Time;
using Rebus.Timeouts;

namespace Rebus.Persistence.FileSystem
{
    /// <summary>
    /// Timeouts in the filesystem!
    /// </summary>
    public class FilesystemTimeoutManager : ITimeoutManager
    { 
        private readonly string _basePath;
        private readonly IRebusLoggerFactory _loggerFactory;
        private readonly string _lockFile;
        private static readonly string _tickFormat;
        
        static FilesystemTimeoutManager()
        {
            _tickFormat = new StringBuilder().Append('0', Int32.MaxValue.ToString().Length).ToString();
        }

        public FilesystemTimeoutManager(string basePath, IRebusLoggerFactory loggerFactory)
        {
            _basePath = basePath;
            _loggerFactory = loggerFactory;
            _lockFile = Path.Combine(basePath, "lock.txt");
            
        }


        public class Timeout
        {
            public Dictionary<string, string> Headers { get; set; }
            public byte[] Body { get; set; }
        }
        public async Task Defer(DateTimeOffset approximateDueTime, Dictionary<string, string> headers, byte[] body)
        {
            using (new FilesystemExclusiveLock(_lockFile, _loggerFactory))
            {
                var prefix = approximateDueTime.UtcDateTime.Ticks.ToString(_tickFormat);
                var count = Directory.EnumerateFiles(_basePath, prefix + "*.json").Count();
                var fileName = Path.Combine(_basePath, $"{prefix}_{count}.json");
                System.IO.File.WriteAllText(fileName, JsonConvert.SerializeObject(new Timeout
                {
                    Headers = headers,
                    Body = body
                }));
            }
        }

        public async Task<DueMessagesResult> GetDueMessages()
        {
            var lockItem = new FilesystemExclusiveLock(_lockFile, _loggerFactory);
            var prefix = RebusTime.Now.UtcDateTime.Ticks.ToString(_tickFormat);
            var enumerable = Directory.EnumerateFiles(_basePath, "*.json")
                .Where(x => String.CompareOrdinal(prefix, 0, Path.GetFileNameWithoutExtension(x), 0, _tickFormat.Length) >= 0)
                .ToList()
               ;

            var items = enumerable
                 
                .Select(f => new
                {
                    Timeout = JsonConvert.DeserializeObject<Timeout>(File.ReadAllText(f)),
                    File = f
                })
                .Select(a => new DueMessage(a.Timeout.Headers, a.Timeout.Body, () =>
                {
                    if (File.Exists(a.File))
                    {
                        File.Delete(a.File);
                    }
                })).ToList();
            return new DueMessagesResult(items, () => {lockItem.Dispose(); });
        }
    }
}