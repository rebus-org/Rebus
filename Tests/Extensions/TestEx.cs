using System;

namespace Tests.Extensions
{
    public static class TestEx
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