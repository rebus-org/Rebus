using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Profiling
{
    /// <summary>
    /// Stats collector
    /// </summary>
    public class PipelineStepProfilerStats
    {
        readonly ConcurrentDictionary<Type, TimeSpan> _stats = new ConcurrentDictionary<Type, TimeSpan>();

        internal void Register(StatsContext statsContext)
        {
            var measurements = statsContext.GetMeasurements();

            foreach (var measurement in measurements)
            {
                _stats.AddOrUpdate(measurement.StepType,
                    key => measurement.Elapsed,
                    (key, elapsed) => elapsed + measurement.Elapsed);
            }
        }

        /// <summary>
        /// Gets the current stats and resets the collector
        /// </summary>
        public List<StepStat> GetAndResetStats()
        {
            var stats = GetStats();
            _stats.Clear();
            return stats;
        }

        /// <summary>
        /// Gets the current stats
        /// </summary>
        public List<StepStat> GetStats()
        {
            return _stats.Select(kvp => new StepStat(kvp.Key, kvp.Value)).ToList();
        }

        /// <summary>
        /// Represents an aggregation of measurements
        /// </summary>
        public class StepStat
        {
            internal StepStat(Type stepType, TimeSpan elapsed)
            {
                if (stepType == null) throw new ArgumentNullException(nameof(stepType));
                StepType = stepType;
                Elapsed = elapsed;
            }

            /// <summary>
            /// Type of step for which this particular statistic was collected
            /// </summary>
            public Type StepType { get; }
            
            /// <summary>
            /// Time spent
            /// </summary>
            public TimeSpan Elapsed { get; }

            /// <summary>
            /// Gets a string representation of this stat on the form "type: elapsed"
            /// </summary>
            public override string ToString() => $"{StepType}: {Elapsed}";
        }
    }
}