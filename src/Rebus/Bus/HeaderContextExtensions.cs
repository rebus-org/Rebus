using System;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Bus
{
    static class HeaderContextExtensions
    {
        public static Dictionary<string, object> GetOrAdd(this List<Tuple<WeakReference, Dictionary<string, object>>> contexts, object key, Func<Dictionary<string, object>> factory)
        {
            var entry = contexts.FirstOrDefault(c => c.Item1.Target == key);
            if (entry == null)
            {
                lock(contexts)
                {
                    entry = contexts.FirstOrDefault(c => c.Item1.Target == key);

                    if (entry == null)
                    {
                        entry = Tuple.Create(new WeakReference(key), factory());
                        contexts.Add(entry);
                    }
                }
            }
            return entry.Item2;
        }

        public static void RemoveDeadReferences(this List<Tuple<WeakReference, Dictionary<string, object>>> contexts)
        {
            if (contexts.Any(c => !c.Item1.IsAlive))
            {
                lock (contexts)
                {
                    contexts.RemoveAll(c => !c.Item1.IsAlive);
                }
            }
        }

        public static bool TryGetValue(this List<Tuple<WeakReference, Dictionary<string, object>>> contexts, object key, out Dictionary<string, object> dictionery)
        {
            var entry = contexts.FirstOrDefault(c => c.Item1.Target == key);

            if (entry == null)
            {
                dictionery = new Dictionary<string, object>();
                return false;
            }
            
            dictionery = entry.Item2;
            return true;
        }
    }
}