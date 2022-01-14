using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Rebus.Pipeline;

namespace Rebus.Profiling;

class StatsContext
{
    readonly ConcurrentStack<Measurement> _measurements = new ConcurrentStack<Measurement>();

    internal void AddMeasurement(IIncomingStep step, TimeSpan elapsed)
    {
        var correctedElapsed = Correct(elapsed);
        var stepType = step.GetType();

        var measurement = new Measurement(stepType, correctedElapsed);

        _measurements.Push(measurement);
    }

    TimeSpan Correct(TimeSpan elapsed) => _measurements.TryPeek(out var previousMeasurement)
        ? elapsed - previousMeasurement.Elapsed
        : elapsed;

    internal IDisposable Measure(IIncomingStep nextStep) => new StatsContextDisposable(this, nextStep);

    class StatsContextDisposable : IDisposable
    {
        readonly StatsContext _statsContext;
        readonly IIncomingStep _nextStep;
        readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        public StatsContextDisposable(StatsContext statsContext, IIncomingStep nextStep)
        {
            _statsContext = statsContext;
            _nextStep = nextStep;
        }

        public void Dispose() => _statsContext.AddMeasurement(_nextStep, _stopwatch.Elapsed);
    }

    public class Measurement
    {
        public Type StepType { get; }
        public TimeSpan Elapsed { get; }

        public Measurement(Type stepType, TimeSpan elapsed)
        {
            StepType = stepType;
            Elapsed = elapsed;
        }

        public override string ToString() => $"{StepType}: {Elapsed}";
    }

    public IEnumerable<Measurement> GetMeasurements()
    {
        var list = _measurements.ToList();
        list.Reverse();
        return list;
    }
}