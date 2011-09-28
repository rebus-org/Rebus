using System;

namespace Rebus.Tests
{
    public static class TestExtensions
    {
        public static void Times(this int count, Action action)
        {
            for (var counter = 0; counter < count; counter++)
            {
                action();
            }
        }
    }
}