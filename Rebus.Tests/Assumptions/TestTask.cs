using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Tests.Contracts;

namespace Rebus.Tests.Assumptions;

[TestFixture]
public class TestTask : FixtureBase
{
    [Test]
    public async Task CanCancelTask()
    {
        var cancellationTokenSource = Using(new CancellationTokenSource());
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
        await Task.Delay(2000, CancellationToken.None);

        cancellationTokenSource.Cancel();

        Console.WriteLine(task.Status);

        await task;

        Console.WriteLine(task.Status);

        Console.WriteLine(string.Join(Environment.NewLine, events));
    }
}