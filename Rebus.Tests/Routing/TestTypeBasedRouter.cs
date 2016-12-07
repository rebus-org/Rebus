using System;
using System.Collections.Generic;
using System.Linq;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Xunit;

namespace Rebus.Tests.Routing
{
    public class TestTypeBasedRouter : FixtureBase
    {
        TypeBasedRouter _router;
        ListLoggerFactory _loggerFactory;

        public TestTypeBasedRouter()
        {
            _loggerFactory = new ListLoggerFactory(detailed: true, outputToConsole: true);
            _router = new TypeBasedRouter(_loggerFactory);
        }

        [Fact]
        public void ThrowsByDefaultWhenRoutingUnmappedTopic()
        {
            var aggregateException = Assert.Throws<AggregateException>(() =>
            {
                _router.GetDestinationAddress(new Message(NoHeaders, "STRING BODY")).Wait();
            });

            var baseException = aggregateException.GetBaseException();

            Console.WriteLine(baseException);

            Assert.IsType<ArgumentException>(baseException);
        }

        [Fact]
        public void CanGetRouteWhenTypeHasBeenMapped()
        {
            _router.Map<string>("some_endpoint");

            var address = GetDestinationForBody("STRING BODY");

            Assert.Equal("some_endpoint", address);
        }

        [Fact]
        public void LogsWarningWhenRouteIsOverwritten()
        {
            _router.Map<string>("some_endpoint");
            _loggerFactory.Clear();

            _router.Map<string>("another_endpoint");

            var logLines = _loggerFactory
                .Where(l => l.Level == LogLevel.Warn)
                .ToList();

            Assert.Equal(1, logLines.Count);
        }

        [Fact]
        public void WorksWithMultipleRoutes ()
        {
            _router
                .Map<string>("StringDestination")
                .Map<DateTime>("DateTimeDestination")
                .Map<int>("IntDestination");

            Assert.Equal("StringDestination", GetDestinationForBody("STRING BODY"));
            Assert.Equal("DateTimeDestination", GetDestinationForBody(DateTime.Now));
            Assert.Equal("IntDestination", GetDestinationForBody(78));
        }

        [Fact]
        public void SupportsFallback()
        {
            _router
                .Map<string>("StringEndpoint")
                .Map<DateTime>("DateTimeEndpoint")
                .MapFallback("fallback");

            Assert.Equal("StringEndpoint",GetDestinationForBody("hej"));
            Assert.Equal("fallback", GetDestinationForBody(87843784));
        }

        [Fact]
        public void LogsWarningWhenFallbackIsOverwritten()
        {
            _router.MapFallback("something");
            _loggerFactory.Clear();

            _router.MapFallback("something_else");

            var logLines = _loggerFactory
                .Where(l => l.Level == LogLevel.Warn)
                .ToList();

            Assert.Equal(1, logLines.Count);
        }

        string GetDestinationForBody(object messageBody)
        {
            return _router.GetDestinationAddress(new Message(NoHeaders, messageBody)).Result;
        }

        static Dictionary<string, string> NoHeaders => new Dictionary<string, string>();
    }
}