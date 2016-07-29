// ReSharper disable ForCanBeConvertedToForeach
namespace Rebus.RabbitMq
{
    static class Knuth
    {
        public static ulong CalculateHash(string read)
        {
            var hashedValue = 3074457345618258791ul;
            for (var i = 0; i < read.Length; i++)
            {
                hashedValue += read[i];
                hashedValue *= 3074457345618258799ul;
            }
            return hashedValue;
        }
    }
}