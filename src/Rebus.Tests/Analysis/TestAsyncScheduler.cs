using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Task = System.Threading.Tasks.Task;

namespace Rebus.Tests.Analysis
{
    [TestFixture]
    public class TestAsyncScheduler
    {
        [Test]
        public async Task VerifyCanTransferThreadBoundThingieToContinuation()
        {
            var list = await AsyncMeth();
            var first = list.First();
            
            Assert.That(list.All(i => i == first), Is.True, "Not all were the same: {0}", string.Join(", ", list));
        }

        static int counter = 1;
        
        [ThreadStatic] static int? someThreadBoundValue;

        static void AddThreadId(List<string> list)
        {
            if (!someThreadBoundValue.HasValue)
            {
                someThreadBoundValue = GetNextValue();
            }

            list.Add(someThreadBoundValue.ToString());
        }

        static int GetNextValue()
        {
            return Interlocked.Increment(ref counter);
        }

        async Task<List<string>> AsyncMeth()
        {
            var list = new List<string>();

            AddThreadId(list);
            await Task.Delay(100);
            AddThreadId(list);
            await Task.Delay(100);
            AddThreadId(list);
            await Task.Delay(100);
            AddThreadId(list);
            await Task.Delay(100);
            AddThreadId(list);
            await Task.Delay(100);
            AddThreadId(list);
            await Task.Delay(100);
            AddThreadId(list);
            await Task.Delay(100);
            AddThreadId(list);
            await Task.Delay(100);
            AddThreadId(list);
            await Task.Delay(100);
            AddThreadId(list);
            await Task.Delay(100);
            AddThreadId(list);
            await Task.Delay(100);
            AddThreadId(list);
            await Task.Delay(100);
            AddThreadId(list);
            await Task.Delay(100);
            AddThreadId(list);
            await Task.Delay(100);
            AddThreadId(list);
            await Task.Delay(100);

            return list;
        }
    }
}