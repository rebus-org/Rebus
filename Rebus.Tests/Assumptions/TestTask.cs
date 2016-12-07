using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Rebus.Tests.Assumptions
{
    public class TestTask
    {
        [Fact]
        public async Task CanCancelTask()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            var events = new List<string>();

            var task = Task.Run(async () =>
            {
                try
                {
                    events.Add("task started");

                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        events.Add("waiting...");
                        await Task.Delay(1000, cancellationToken);
                    }

                }
                catch (Exception exception)
                {
                    events.Add(exception.ToString());
                }
            }, cancellationToken);

            //Console.WriteLine(task.Status);
            await Task.Delay(2000);

            cancellationTokenSource.Cancel();

            Console.WriteLine(task.Status);

            await task;

            Console.WriteLine(task.Status);

            Console.WriteLine(string.Join(Environment.NewLine, events));
        }
    }
}