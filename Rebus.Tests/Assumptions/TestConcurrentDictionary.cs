using System;
using System.Collections.Concurrent;
using System.Threading;
using NUnit.Framework;

namespace Rebus.Tests.Assumptions;

[TestFixture]
public class TestConcurrentDictionary
{
    [Test]
    public void NizzleName()
    {
        var dictionary = new ConcurrentDictionary<string, string>();

        ThreadPool.QueueUserWorkItem(__ =>
        {
            dictionary.GetOrAdd("bimse", _ =>
            {
                Console.WriteLine("waiting");
                Thread.Sleep(1000);
                Console.WriteLine("done");
                return "hej";
            });
        });

        ThreadPool.QueueUserWorkItem(__ =>
        {
            dictionary.GetOrAdd("bimse", _ =>
            {
                Console.WriteLine("waiting");
                Thread.Sleep(1000);
                Console.WriteLine("done");
                return "hej";
            });
        });

        Thread.Sleep(3000);
    }
}