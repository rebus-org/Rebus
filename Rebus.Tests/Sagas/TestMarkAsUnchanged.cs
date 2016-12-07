using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Sagas;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Sagas
{
    public class TestMarkAsUnchanged : FixtureBase
    {
        readonly BuiltinHandlerActivator _activator;

        public TestMarkAsUnchanged()
        {
            _activator = Using(new BuiltinHandlerActivator());

            Configure.With(_activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "test saga things"))
                .Options(o =>
                {
                    o.SetMaxParallelism(1);
                    o.SetNumberOfWorkers(1);
                })
                .Start();
        }

        [Fact]
        public async Task CanMarkSagaAsUnchanged()
        {
            var registeredRevisions = new ConcurrentQueue<int>();
            _activator.Register(() => new SomeSaga(registeredRevisions));

            await _activator.Bus.SendLocal("1/hej");
            await _activator.Bus.SendLocal("1/med");
            await _activator.Bus.SendLocal("1/dig");
            await _activator.Bus.SendLocal("1/min");
            await _activator.Bus.SendLocal("1/ven");

            await Task.Delay(1000);

            await _activator.Bus.SendLocal("1/ignore!");

            await Task.Delay(1000);

            await _activator.Bus.SendLocal("1/hej");
            await _activator.Bus.SendLocal("1/igen");

            await Task.Delay(1000);

            Assert.Equal(new[] { 0, 0, 1, 2, 3, 4, 4, 5 }, registeredRevisions.ToArray());
        }

        class SomeSaga : Saga<SomeSagaData>, IAmInitiatedBy<string>
        {
            readonly ConcurrentQueue<int> _registeredRevisions;

            public SomeSaga(ConcurrentQueue<int> registeredRevisions)
            {
                _registeredRevisions = registeredRevisions;
            }

            protected override void CorrelateMessages(ICorrelationConfig<SomeSagaData> config)
            {
                config.Correlate<string>(GetString, d => d.String);
            }

            public async Task Handle(string message)
            {
                Console.WriteLine($"Handling '{message}' on thread {Thread.CurrentThread.ManagedThreadId}");

                Data.String = GetString(message);
                Data.InvocationCount++;

                Console.Write($"REVISION {Data.Revision} - ");
                _registeredRevisions.Enqueue(Data.Revision);

                if (message.EndsWith("ignore!"))
                {
                    Console.WriteLine("MARKED AS UNCHANGED!!");
                    MarkAsUnchanged();
                }
                else
                {
                    Console.WriteLine("...");
                }
            }

            static string GetString(string m)
            {
                return m.Split('/').First();
            }
        }

        class SomeSagaData : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }

            public string String { get; set; }
            public int InvocationCount { get; set; }
        }
    }

}