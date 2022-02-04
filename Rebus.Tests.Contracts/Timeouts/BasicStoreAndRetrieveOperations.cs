using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Timeouts;

namespace Rebus.Tests.Contracts.Timeouts;

/// <summary>
/// Test fixture base class for verifying compliance with the <see cref="ITimeoutManager"/> contract
/// </summary>
public abstract class BasicStoreAndRetrieveOperations<TTimeoutManagerFactory> : FixtureBase where TTimeoutManagerFactory : ITimeoutManagerFactory, new()
{
    TTimeoutManagerFactory _factory;
    ITimeoutManager _timeoutManager;

    protected override void SetUp()
    {
        _factory = new TTimeoutManagerFactory();
        _timeoutManager = _factory.Create();
    }

    protected override void TearDown()
    {
        _factory.Cleanup();
    }

    [Test]
    public async Task DoesNotLoadAnythingInitially()
    {
        using (var result = await _timeoutManager.GetDueMessages())
        {
            Assert.That(result.Count(), Is.EqualTo(0));
        }
    }

    [Test]
    public async Task IsCapableOfReturningTheMessageBodyAsWell()
    {
        var someTimeInThePast = DateTime.Now.AddMinutes(-1);

        const string stringBody = "hello there!";

        await _timeoutManager.Defer(someTimeInThePast, HeadersWith("i know u"), Encoding.UTF8.GetBytes(stringBody));

        using (var result = await _timeoutManager.GetDueMessages())
        {
            var dueTimeouts = result.ToList();

            Assert.That(dueTimeouts.Count, Is.EqualTo(1));
                
            var bodyBytes = dueTimeouts[0].Body;
                
            Assert.That(Encoding.UTF8.GetString(bodyBytes), Is.EqualTo(stringBody));
        }
    }

    [Test]
    public async Task ImmediatelyGetsTimeoutWhenItIsDueInThePast()
    {
        var someTimeInThePast = DateTime.Now.AddMinutes(-1);

        await _timeoutManager.Defer(someTimeInThePast, HeadersWith("i know u"), EmptyBody());

        using (var result = await _timeoutManager.GetDueMessages())
        {
            var dueTimeouts = result.ToList();

            Assert.That(dueTimeouts.Count, Is.EqualTo(1));
            Assert.That(dueTimeouts[0].Headers[Headers.MessageId], Is.EqualTo("i know u"));
        }
    }

    [Test]
    public async Task TimeoutsAreNotRemovedIfTheyAreNotMarkedAsComplete()
    {
        var theFuture = DateTime.Now.AddMinutes(1);

        await _timeoutManager.Defer(theFuture, HeadersWith("i know u"), EmptyBody());
            
        _factory.FakeIt(theFuture + TimeSpan.FromSeconds(1));

        using (var result = await _timeoutManager.GetDueMessages())
        {
            var dueTimeoutsInTheFuture = result.ToList();
            Assert.That(dueTimeoutsInTheFuture.Count, Is.EqualTo(1), "Did not get the expected number of timeouts - debug info: {0}", _factory.GetDebugInfo());
        }

        using (var result = await _timeoutManager.GetDueMessages())
        {
            var dueTimeoutsInTheFuture = result.ToList();
            Assert.That(dueTimeoutsInTheFuture.Count, Is.EqualTo(1), "Did not get the expected number of timeouts - debug info: {0}", _factory.GetDebugInfo());
            
            // mark as complete
            await dueTimeoutsInTheFuture[0].MarkAsCompleted();
        }
            
        using (var result = await _timeoutManager.GetDueMessages())
        {
            var dueTimeoutsInTheFuture = result.ToList();
            Assert.That(dueTimeoutsInTheFuture.Count, Is.EqualTo(0), "Did not get the expected number of timeouts - debug info: {0}", _factory.GetDebugInfo());
        }
    }

    [Test]
    public async Task TimeoutsAreNotReturnedUntilTheyAreActuallyDue()
    {
        var theFuture = DateTimeOffset.Now.AddMinutes(1);
        var evenFurtherIntoTheFuture = DateTimeOffset.Now.AddMinutes(8);

        await _timeoutManager.Defer(theFuture, HeadersWith("i know u"), EmptyBody());
        await _timeoutManager.Defer(evenFurtherIntoTheFuture, HeadersWith("i know u too"), EmptyBody());

        using (var result = await _timeoutManager.GetDueMessages())
        {
            var dueTimeoutsNow = result.ToList();

            Assert.That(dueTimeoutsNow.Count, Is.EqualTo(0), 
                $"Didn't expect any due messages at this point, because the time is {DateTimeOffset.Now}, and the messages were deferred to the times {theFuture} and {evenFurtherIntoTheFuture}");
        }

        _factory.FakeIt(theFuture);

        using (var result = await _timeoutManager.GetDueMessages())
        {
            var dueTimeoutsInTheFuture = result.ToList();
                
            Assert.That(dueTimeoutsInTheFuture.Count, Is.EqualTo(1), 
                $"Expected one due message at this point, because the time is {theFuture}, and the messages were deferred to the times {theFuture} and {evenFurtherIntoTheFuture}");

            var dueMessage = dueTimeoutsInTheFuture[0];

            Assert.That(dueMessage.Headers[Headers.MessageId], Is.EqualTo("i know u"));

            await dueMessage.MarkAsCompleted();
        }

        _factory.FakeIt(evenFurtherIntoTheFuture);

        using (var result = await _timeoutManager.GetDueMessages())
        {
            var dueTimeoutsFurtherIntoInTheFuture = result.ToList();
            Assert.That(dueTimeoutsFurtherIntoInTheFuture.Count, Is.EqualTo(1),
                $"Expected one due message at this point, because the time is {evenFurtherIntoTheFuture}, and the messages were deferred to the times {theFuture} and {evenFurtherIntoTheFuture} (but the one deferred to {theFuture} was marked as completed)");
                
            var dueMessage = dueTimeoutsFurtherIntoInTheFuture[0];
                
            Assert.That(dueMessage.Headers[Headers.MessageId], Is.EqualTo("i know u too"));

            await dueMessage.MarkAsCompleted();
        }
    }

    static Dictionary<string, string> HeadersWith(string id)
    {
        return new Dictionary<string, string>
        {
            { Headers.MessageId, id }
        };
    }

    static byte[] EmptyBody()
    {
        return new byte[0];
    }
}