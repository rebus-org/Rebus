using System;
using System.Collections.Generic;
using System.Linq;
using Rebus.Config;
using Rebus.Workers.ThreadPoolBased;

namespace Rebus.Backoff;

/// <summary>
/// Configuration extensions for customizing the backoff behavior
/// </summary>
public static class BackoffConfigurationExtensions
{
    /// <summary>
    /// Configures the timespans to wait when backing off polling the transport during idle times. <paramref name="backoffTimes"/>
    /// must be a sequence of timespans, which indicates the time to wait for each second elapsed being idle. When the idle time
    /// exceeds the number of timespans, the last timespan will be used.
    /// </summary>
    public static void SetBackoffTimes(this OptionsConfigurer configurer, params TimeSpan[] backoffTimes)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));
        if (backoffTimes == null) throw new ArgumentNullException(nameof(backoffTimes));

        SetBackoffTimes(configurer, (IEnumerable<TimeSpan>)backoffTimes);
    }

    /// <summary>
    /// Configures the timespans to wait when backing off polling the transport during idle times. <paramref name="backoffTimes"/>
    /// must be a sequence of timespans, which indicates the time to wait for each second elapsed being idle. When the idle time
    /// exceeds the number of timespans, the last timespan will be used.
    /// </summary>
    public static void SetBackoffTimes(this OptionsConfigurer configurer, IEnumerable<TimeSpan> backoffTimes)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));
        if (backoffTimes == null) throw new ArgumentNullException(nameof(backoffTimes));

        var list = backoffTimes.ToList();

        if (!list.Any())
        {
            throw new ArgumentException("Please specify at least one TimeSpan when you customize the backoff times! You could for example specify new[] { TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5) } in order to wait 0.5s and 1s during the first two seconds of inactivity, and then 5 seconds poll interval forever thereafter");
        }

        configurer.Register<IBackoffStrategy>(c => new DefaultBackoffStrategy(list, c.Get<Options>()));
    }
}