using System;
using System.Linq;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;
using Xunit;

namespace Rebus.Tests.Pipeline
{
    public class TestPipelineLogging : FixtureBase
    {
        readonly ListLoggerFactory _listLoggerFactory;

        public TestPipelineLogging()
        {
            _listLoggerFactory = new ListLoggerFactory();
        }

        [Fact]
        public void CanLogPipelineGood()
        {
            var bus = Configure.With(new BuiltinHandlerActivator())
                .Logging(l => l.Use(_listLoggerFactory))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "test"))
                .Options(o => o.LogPipeline(verbose:true))
                .Start();

            Using(bus);

            var listLoggerFactory = _listLoggerFactory;

            Console.WriteLine(string.Join(Environment.NewLine, listLoggerFactory.Select(l => l.Text)));
            Console.WriteLine();
        }
    }
}