using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport;

namespace Rebus.Tests.Transactions
{
    [TestFixture]
    public class TestTransactionContextPerformance
    {
        [Test]
        public async Task CheckHowLongItTakes()
        {
            var stopwatch = Stopwatch.StartNew();

            100000.Times(() =>
            {
                using (var transactionContext = new DefaultTransactionContext())
                {
                    transactionContext.Complete().Wait();
                }

            });

            var elapsed = stopwatch.Elapsed.TotalSeconds;

            Console.WriteLine($"Elapsed: {elapsed:0.0} s");
        }
    }
}