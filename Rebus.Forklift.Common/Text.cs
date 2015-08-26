using System;

namespace Rebus.Forklift.Common
{
    public static class Text
    {
        public static void PrintLine()
        {
            Console.WriteLine();
        }

        public static void PrintLine(string message, params object[] objs)
        {
            Console.WriteLine(message, objs);
        }

        public static void Print(string message, params object[] objs)
        {
            Console.Write(message, objs);
        }

    }
}