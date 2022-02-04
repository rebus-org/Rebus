using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Rebus.Tests.Contracts.Utilities;

public static class DeleteHelper
{
    /// <summary>
    /// Recursively deletes the directory and everything in it, trying a couple of times if it fails
    /// </summary>
    public static void DeleteDirectory(string path)
    {
        var stopwatch = Stopwatch.StartNew();
        var timeout = TimeSpan.FromSeconds(10);
        var attempts = 0;

        while (true)
        {
            try
            {
                attempts++;

                if (!Directory.Exists(path)) return;

                Directory.Delete(path, true);

                if (attempts > 1)
                {
                    Console.WriteLine($"Succeeded after {attempts} attempts");
                }

                return;
            }
            catch (Exception exception)
            {
                if (stopwatch.Elapsed > timeout)
                {
                    throw new IOException($"Could not delete {path} in {attempts} attempts within {timeout} timeout", exception);
                }

                Console.WriteLine($"Could not delete {path} in {attempts} attempts - waiting 500 ms....");

                Thread.Sleep(500);
            }
        }

    }
}