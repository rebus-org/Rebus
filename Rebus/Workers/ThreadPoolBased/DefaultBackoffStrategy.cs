using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Config;

namespace Rebus.Workers.ThreadPoolBased;

class DefaultBackoffStrategy : IBackoffStrategy
{
    readonly TimeSpan[] _backoffTimes;
    readonly Options _options;

    long _waitTimeTicks;

    /// <summary>
    /// Constructs the backoff strategy with the given waiting times
    /// </summary>
    public DefaultBackoffStrategy(IEnumerable<TimeSpan> backoffTimes, Options options)
    {
        if (backoffTimes == null) throw new ArgumentNullException(nameof(backoffTimes));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        _backoffTimes = backoffTimes.ToArray();

        if (_backoffTimes.Length < 1)
        {
            throw new ArgumentException("Cannot construct customized backoff strategy without specifying at least one wait time!");
        }
    }

    /// <param name="token"></param>
    /// <inheritdoc />
    public void Wait(CancellationToken token)
    {
        InnerWait(token);
    }

    /// <param name="token"></param>
    /// <inheritdoc />
    public Task WaitAsync(CancellationToken token)
    {
        return InnerWaitAsync(token);
    }

    /// <param name="token"></param>
    /// <inheritdoc />
    public void WaitNoMessage(CancellationToken token)
    {
        InnerWait(token);
    }

    /// <param name="token"></param>
    /// <inheritdoc />
    public Task WaitNoMessageAsync(CancellationToken token)
    {
        return InnerWaitAsync(token);
    }

    /// <param name="token"></param>
    /// <inheritdoc />
    public void WaitError(CancellationToken token)
    {
        var cooldownTime = _options.TransportReceiveErrorCooldownTime;

        token.WaitHandle.WaitOne(cooldownTime);
    }

    /// <param name="token"></param>
    /// <inheritdoc />
    public async Task WaitErrorAsync(CancellationToken token)
    {
        var cooldownTime = _options.TransportReceiveErrorCooldownTime;

        await Task.Delay(cooldownTime, token);
    }

    /// <inheritdoc />
    public void Reset()
    {
        Interlocked.Exchange(ref _waitTimeTicks, 0);
    }

    async Task InnerWaitAsync(CancellationToken token)
    {
        var backoffTime = GetNextBackoffTime();

        await Task.Delay(backoffTime, token);
    }

    void InnerWait(CancellationToken token)
    {
        var backoffTime = GetNextBackoffTime();

        token.WaitHandle.WaitOne(backoffTime);
    }

    TimeSpan GetNextBackoffTime()
    {
        var waitedSinceTicks = Interlocked.Read(ref _waitTimeTicks);

        if (waitedSinceTicks == 0)
        {
            waitedSinceTicks = DateTime.UtcNow.Ticks;
            Interlocked.Exchange(ref _waitTimeTicks, waitedSinceTicks);
        }

        var waitDurationTicks = DateTime.UtcNow.Ticks - waitedSinceTicks;
        var totalSecondsIdle = (int) TimeSpan.FromTicks(waitDurationTicks).TotalSeconds;
        var waitTimeIndex = Math.Max(0, Math.Min(totalSecondsIdle, _backoffTimes.Length - 1));

        var backoffTime = _backoffTimes[waitTimeIndex];

        return backoffTime;
    }
}