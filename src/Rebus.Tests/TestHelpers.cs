using System;
using System.Linq;
using NUnit.Framework;

namespace Rebus.Tests
{
    public class TestHelpers
    {
        public static void DumpHeadersFromCurrentMessageContext()
        {
            if (!MessageContext.HasCurrent)
            {
                Assert.Fail("No message context!!!");
            }

            Console.WriteLine("----------------------------------------------------------------------------------");
            Console.WriteLine(string.Join(Environment.NewLine, MessageContext.GetCurrent().Headers.Select(h => string.Format("    {0} = {1}", h.Key, h.Value))));
            Console.WriteLine("----------------------------------------------------------------------------------");
        }
    }
}