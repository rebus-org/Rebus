using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Routing.Exceptions;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Tests.Extensions;
using Rebus.Transport.InMem;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Integration
{
    public class TestRetryExceptionCustomization : FixtureBase
    {
        const int SecretErrorCode = 340;
        readonly BuiltinHandlerActivator _activator;
        readonly ListLoggerFactory _listLoggerFactory;

        public TestRetryExceptionCustomization()
        {
            _activator = Using(new BuiltinHandlerActivator());
            _listLoggerFactory = new ListLoggerFactory();

            Configure.With(_activator)
                .Logging(l => l.Use(_listLoggerFactory))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "customize exceptions"))
                .Routing(r =>
                {
                    // Commented out the following line as this was causing the 'PerformsTheUsualRetriesOnExceptionsThatDoNotSatisfyThePredicate'
                    // test to keep on failing. In the original code base an exception was an ApplicationExcpetion by default, but this is now being 
                    // considered bad practice. Therefore the basic exception type if 'Exception', but all exception derive from that type. So basically,
                    // the r.ForwardOnException<Exception>("error", LogLevel.Error) always matched. 
                    //r.ForwardOnException<Exception>("error", LogLevel.Error);

                    r.ForwardOnException<CustomException>("error", LogLevel.Error, e =>
                        {
                            Console.WriteLine("Checking {0}", e);
                            return e.ErrorCode == SecretErrorCode;
                        });
                })
                .Options(o => o.LogPipeline(verbose: true))
                .Start();
        }

        [Fact]
        public async Task OnlyLogsOneSingleLineWhenForwarding()
        {
            _activator.Handle<ShouldFail>(async msg =>
            {
                throw new CustomException { ErrorCode = SecretErrorCode };
            });

            await _activator.Bus.SendLocal(new ShouldFail());

            await Task.Delay(2000);

            var significantStuff = _listLoggerFactory.Where(l => l.Level >= LogLevel.Warn).ToList();

            Console.WriteLine(string.Join(Environment.NewLine, significantStuff.Select(l => l.Text.Limit(140, singleLine: true))));

            // if it fails: Only expected one single ERROR level log line with all the action
            Assert.Equal(1, significantStuff.Count);
        }

        [Fact]
        public async Task MakesOnlyOneSingleDeliveryAttemptWhenForwardingOnExceptionThatSatisfiesPredicate()
        {
            var deliveryAttempts = 0;

            _activator.Handle<ShouldFail>(async msg =>
            {
                Interlocked.Increment(ref deliveryAttempts);

                throw new CustomException { ErrorCode = SecretErrorCode };
            });

            await _activator.Bus.SendLocal(new ShouldFail());

            await Task.Delay(2000);

            // if it fails: Only expected one single delivery attempt because we threw a CustomException with ErrorCode = SecretErrorCode
            Assert.Equal(1, deliveryAttempts);
        }

        [Fact]
        public async Task PerformsTheUsualRetriesOnExceptionsThatDoNotSatisfyThePredicate()
        {
            var deliveryAttempts = 0;

            _activator.Handle<ShouldFail>(async msg =>
            {
                Interlocked.Increment(ref deliveryAttempts);
                throw new CustomException { ErrorCode = SecretErrorCode + 23 };
            });

            await _activator.Bus.SendLocal(new ShouldFail());

            await Task.Delay(2000);

            // if it fails: Expected the usual retries because we threw a CustomException that did not satisfy the predicate
            Assert.Equal(5, deliveryAttempts);
        }

        class ShouldFail
        {
        }

        class CustomException : Exception
        {
            public int ErrorCode { get; set; }
        }
    }
}