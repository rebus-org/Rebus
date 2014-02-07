using System;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Tests
{
    static class DisposableTracker
    {
        static readonly List<IDisposable> TrackedDisposables = new List<IDisposable>();

        public static void TrackDisposable<T>(T disposableToTrack) where T : IDisposable
        {
            TrackedDisposables.Add(disposableToTrack);
        }

        /// <summary>
        /// Disposes all the disposables and empties the list
        /// </summary>
        public static void DisposeTheDisposables()
        {
            var disposables = TrackedDisposables.ToList();
            TrackedDisposables.Clear();

            foreach (var disposable in disposables)
            {
                try
                {
                    Console.WriteLine("Disposing {0}", disposable);
                    disposable.Dispose();
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred while disposing {0}: {1}", disposable, e);
                }
            }
        }

        public static IEnumerable<IDisposable> GetTrackedDisposables()
        {
            return TrackedDisposables.ToList();
        }
    }
}