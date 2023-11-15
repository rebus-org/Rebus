using System;
using System.Collections.Generic;
using Rebus.Extensions;
// ReSharper disable EmptyGeneralCatchClause

namespace Rebus.DataBus;

/// <summary>
/// Helper method for data bus stuff
/// </summary>
static class DataBusStorageQuery
{
    /// <summary>
    /// Gets whether the given data bus attachment metadata satisfies the criteria
    /// </summary>
    public static bool IsSatisfied(Dictionary<string, string> metadata, TimeRange readTime, TimeRange saveTime)
    {
        if (metadata == null) throw new ArgumentNullException(nameof(metadata));

        var fromReadTime = readTime?.From;
        var toReadTime = readTime?.To;
        var fromSaveTime = saveTime?.From;
        var toSaveTime = saveTime?.To;

        if (fromReadTime != null || toReadTime != null)
        {
            if (metadata.TryGetValue(MetadataKeys.ReadTime, out var readTimeString))
            {
                try
                {
                    var readTimeDto = readTimeString.ToDateTimeOffset();
                    if (fromReadTime != null && readTimeDto < fromReadTime) return false;
                    if (toReadTime != null && readTimeDto >= toReadTime) return false;
                }
                catch
                {
                }
            }
        }

        if (fromSaveTime != null || toSaveTime != null)
        {
            if (metadata.TryGetValue(MetadataKeys.SaveTime, out var saveTimeString))
            {
                try
                {
                    var saveTimeDto = saveTimeString.ToDateTimeOffset();
                    if (fromSaveTime != null && saveTimeDto < fromSaveTime) return false;
                    if (toSaveTime != null && saveTimeDto >= toSaveTime) return false;
                }
                catch
                {
                }
            }
        }

        return true;
    }
}