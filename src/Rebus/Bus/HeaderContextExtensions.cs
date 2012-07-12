using System;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Bus
{
    static class HeaderContextExtensions
    {
        public static Dictionary<string,string> GetOrAdd(this List<Tuple<WeakReference, Dictionary<string, string>>> contexts, object key, Func<Dictionary<string, string>> factory)
        {
            var entry = contexts.FirstOrDefault(c => c.Item1.Target == key);
            if (entry == null)
            {
                lock(contexts)
                {
                    entry = contexts.FirstOrDefault(c => c.Item1.Target == key);

                    if (entry == null)
                    {
                        entry = new Tuple<WeakReference, Dictionary<string, string>>(new WeakReference(key), factory());
                        contexts.Add(entry);
                    }
                }
            }
            return entry.Item2;
        }

        public static void RemoveDeadReferences(this List<Tuple<WeakReference, Dictionary<string, string>>> contexts)
        {
            if (contexts.Any(c => !c.Item1.IsAlive))
            {
                lock (contexts)
                {
                    contexts.RemoveAll(c => !c.Item1.IsAlive);
                }
            }
        }

        public static bool TryGetValue(this List<Tuple<WeakReference, Dictionary<string,string>>> contexts, object key, out Dictionary<string, string> dictionery)
        {
            var entry = contexts.FirstOrDefault(c => c.Item1.Target == key);

            if (entry == null)
            {
                dictionery = new Dictionary<string, string>();
                return false;
            }
            
            dictionery = entry.Item2;
            return true;
        }
    }
}