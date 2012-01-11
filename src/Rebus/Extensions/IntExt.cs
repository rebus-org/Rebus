using System;

namespace Rebus.Extensions
{
    static class IntExt
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