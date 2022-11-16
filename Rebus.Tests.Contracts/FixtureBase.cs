using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Tests.Contracts.Utilities;

namespace Rebus.Tests.Contracts;

public abstract class FixtureBase
{
    static FixtureBase() => Console.SetOut(TestContext.Progress);

    readonly ConcurrentStack<IDisposable> _disposables = new();

    [SetUp]
    public void _SetUp()
    {
        _disposables.Clear();

        SetUp();
    }

    [TearDown]
    public void _TearDown()
    {
        CleanUpDisposables();

        TearDown();
    }

    protected virtual void SetUp()
    {
    }

    protected virtual void TearDown()
    {
    }

    protected TDisposable Using<TDisposable>(TDisposable disposable) where TDisposable : IDisposable
    {
        _disposables.Push(disposable);
        return disposable;
    }

    protected string NewTempDirectory()
    {
        var directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Guid.NewGuid().ToString("N"));

        Console.WriteLine($"Creating temp directory '{directoryPath}'");
        Directory.CreateDirectory(directoryPath);

        Using(new DisposableCallback(() =>
        {
            Console.WriteLine($"Deleting temp directory '{directoryPath}'");
            DeleteHelper.DeleteDirectory(directoryPath);
        }));

        return directoryPath;
    }

    protected string GetTempFilePath()
    {
        var tempFile = Path.GetTempFileName();
        Using(new FileDeleter(tempFile));
        return tempFile;
    }

    protected Task MeasuredDelay(int milliseconds)
    {
        return MeasuredDelay(TimeSpan.FromMilliseconds(milliseconds));
    }

    protected async Task MeasuredDelay(TimeSpan delay)
    {
        var stopwatch = Stopwatch.StartNew();

        await Task.Delay(delay);

        Console.WriteLine($"Measured delay of {delay} took {stopwatch.Elapsed.TotalSeconds:0.0} s");
    }

    class FileDeleter : IDisposable
    {
        readonly string _filePath;

        public FileDeleter(string filePath)
        {
            _filePath = filePath;
        }

        public void Dispose()
        {
            try
            {
                File.Delete(_filePath);
            }
            catch (FileNotFoundException)
            {
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Could not delete file {_filePath}: {exception}");
            }
        }
    }

    protected void CleanUpDisposables()
    {
        while (_disposables.TryPop(out var disposable))
        {
            Console.WriteLine($"Disposing {disposable}");
            disposable.Dispose();
        }
    }

    /// <summary>
    /// Prints the text with a fairly precise timestamp
    /// </summary>
    public static void Printt(string message)
    {
        var now = DateTime.Now;
        var text = $"{now:HH:mm:ss}.{now.Millisecond:000}: {message}";
        Console.WriteLine(text);
    }
}