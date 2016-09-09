using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Rebus.Msmq.Tests.Assumptions
{
    [TestFixture]
    public class EncodingAssumptions
    {
        [Test]
        public void PrintFirstFiveBytes()
        {
            var @object = new {Text = "hello"};
            var json = JsonConvert.SerializeObject(@object);

            var utf7Bytes = Encoding.UTF7.GetBytes(json);
            var utf8Bytes = Encoding.UTF8.GetBytes(json);

            Console.WriteLine(json);
            Console.WriteLine();
            Print("UTF-7", utf7Bytes);
            Print("UTF-8", utf8Bytes);
        }

        static void Print(string encoding, byte[] bytes)
        {
            var byteString = string.Join(" ", bytes.Take(5).Select(b => (int) b));

            Console.WriteLine($"{encoding}: {byteString}");
        }
    }
}