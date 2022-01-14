using System;

namespace Rebus.Workers;

/// <summary>
/// Represents a worker, which is a thing that is capable of doing work. It may correspond to a worker thread
/// if the usual Rebus worker threads are used, but it may be possible to do other stuff as well
/// </summary>
public interface IWorker : IDisposable
{
    /// <summary>
    /// Gets the name of the worker. Each worker will be named so that they can be recognized
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Signals that the worker should try to stop itself because it will be thrown out and disposed in a little while
    /// </summary>
    void Stop();
}