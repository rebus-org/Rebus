using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Tests.Contracts.Extensions;
#pragma warning disable 1998

namespace Rebus.Tests.Contracts.Transports;

public abstract class TestManyMessages<TBusFactory> : FixtureBase where TBusFactory : IBusFactory, new()
{
    TBusFactory _busFactory;

    protected override void SetUp()
    {
        _busFactory = new TBusFactory();
    }

    protected override void TearDown()
    {
        _busFactory.Cleanup();
    }

    [TestCase(10)]
    public async Task SendAndReceiveManyMessages(int messageCount)
    {
        var allMessagesReceived = new ManualResetEvent(false);
        var idCounts = new ConcurrentDictionary<int, int>();
        var sentMessages = 0;
        var receivedMessages = 0;
        var stopWatch = new Stopwatch();
        var inputQueueAddress = TestConfig.GetName("input1");

        var bus1 = _busFactory.GetBus<MessageWithId>(inputQueueAddress, async msg =>
        {
            idCounts.AddOrUpdate(msg.Id, 1, (id, old) => old + 1);

            Interlocked.Increment(ref receivedMessages);

            if (receivedMessages >= messageCount)
            {
                stopWatch.Stop();
                Console.WriteLine("DONE: took time:" + stopWatch.ElapsedMilliseconds + "ms");
                allMessagesReceived.Set();

            }
        });

        var messagesToSend = Enumerable.Range(0, messageCount)
            .Select(id => new MessageWithId(id))
            .ToList();

        using (BuildPrintTimer(sentMessages, receivedMessages))
        {
            stopWatch.Start();
            Console.WriteLine("Sending {0} messages", messageCount);
            await Task.WhenAll(messagesToSend.Select(async msg =>
            {
                await bus1.SendLocal(msg);
                Interlocked.Increment(ref sentMessages);
            }));

            var timeout = TimeSpan.FromSeconds(messageCount * 0.01 + 100);
            Console.WriteLine("Waiting up to {0} seconds", timeout.TotalSeconds);
            allMessagesReceived.WaitOrDie(timeout, errorMessageFactory: () => GenerateErrorText(idCounts));
        }

        Console.WriteLine("Waiting one more second in case messages are still dripping in...");
        await Task.Delay(1000);

        var errorText = GenerateErrorText(idCounts);

        Assert.That(idCounts.Count, Is.EqualTo(messageCount), errorText);
        Assert.That(idCounts.All(c => c.Value == 1), errorText);
    }

    private Timer BuildPrintTimer(int sentMessages, int receivedMessages)
    {
        return new Timer(
            (object o) => { Console.WriteLine($"Sent: {sentMessages}, Received: {receivedMessages}"); }, null,
            TimeSpan.FromMilliseconds(5000), TimeSpan.FromMilliseconds(5000));
    }

    static string GenerateErrorText(ConcurrentDictionary<int, int> idCounts)
    {
        var errorText =
            $"The following IDs were received != 1 times: {string.Join(", ", idCounts.Where(kvp => kvp.Value != 1).OrderBy(kvp => kvp.Value).Select(kvp => $"{kvp.Key} (x {kvp.Value})"))}";
        return errorText;
    }

    class MessageWithId
    {
        public MessageWithId(int id)
        {
            Id = id;
        }

        public int Id { get; }

        public override string ToString()
        {
            return $"<msg {Id}>";
        }
    }
}