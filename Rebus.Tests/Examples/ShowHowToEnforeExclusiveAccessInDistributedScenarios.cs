using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.ExclusiveLocks;
using Rebus.Persistence.InMem;
using Rebus.Sagas.Exclusive;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Rebus.Tests.Examples;

[TestFixture]
public class ShowHowToEnforeExclusiveAccessInDistributedScenarios : FixtureBase
{
    [Test]
    public async Task HowToConfigureIt()
    {
        using var activator = new BuiltinHandlerActivator();

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new(), "in-mem-queue-1"))
            .Sagas(s =>
            {
                //< in-mem sagas with distributed locks does not make sense, 
                s.StoreInMemory();
                
                s.EnforceExclusiveAccess(new MyDistributedExclusiveAccessLock(TimeSpan.FromSeconds(10)));
            })
            .Start();
    }

    class MyDistributedExclusiveAccessLock(TimeSpan lockAcquisitionTimeout) : IExclusiveAccessLock
    {
        readonly ConcurrentDictionary<string, bool> _acquiredLocks = new();

        public async Task<bool> AcquireLockAsync(string key, CancellationToken cancellationToken)
        {
            using var timeout = new CancellationTokenSource(lockAcquisitionTimeout);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellationToken);

            try
            {
                await InnerAcquireLockAsync(key, cts.Token);
                _acquiredLocks[key] = true;
                return true;
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                return false;
            }
        }

        public async Task<bool> ReleaseLockAsync(string key)
        {
            try
            {
                await InnerReleaseLockAsync(key);
                return true;
            }
            finally
            {
                _acquiredLocks.TryRemove(key, out _);
            }
        }

        public Task<bool> IsLockAcquiredAsync(string key, CancellationToken cancellationToken) => Task.FromResult(_acquiredLocks.TryGetValue(key, out var result) && result);

        async Task InnerAcquireLockAsync(string key, CancellationToken cancellationToken) => throw new NotImplementedException("do actual lock acquisition here");

        async Task InnerReleaseLockAsync(string key) => throw new NotImplementedException("do actual lock release here");
    }
}