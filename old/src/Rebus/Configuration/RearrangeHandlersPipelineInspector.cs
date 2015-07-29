using System;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Configuration
{
    /// <summary>
    /// Simple implementation of <see cref="IInspectHandlerPipeline"/> that is capable of
    /// ordering handlers.
    /// </summary>
    public class RearrangeHandlersPipelineInspector : IInspectHandlerPipeline
    {
        /// <summary>
        /// Note that <see cref="Dictionary{TKey,TValue}"/> is safe for reading from
        /// multiple threads. It is assumed that <see cref="SetOrder"/> is called
        /// during some kind of single-threaded startup phase, whereafter calls to
        /// the filter method may happen in a re-entrant manner.
        /// </summary>
        Dictionary<Type, int> orders = new Dictionary<Type, int>();
        string stackTrace;
        int maxIndex;

        /// <summary>
        /// Implements the filtering operation by attempting to return an ordered sequence of handlers where
        /// the configured ordering of handler types is respected
        /// </summary>
        public IEnumerable<IHandleMessages> Filter(object message, IEnumerable<IHandleMessages> handlers)
        {
            return handlers
                .Select(handler =>
                        new
                            {
                                Handler = handler,
                                Order = orders.ContainsKey(handler.GetType())
                                            ? orders[handler.GetType()]
                                            : maxIndex
                            })
                .OrderBy(a => a.Order)
                .Select(a => a.Handler)
                .ToList();
        }

        /// <summary>
        /// Specifies the desired order of handlers. When this is done, RearrangeHandlersPipelineInspector
        /// will ensure that all handler pipelines containing one or more handlers from <paramref name="handlerTypes"/>
        /// will be ordered so that these handlers come first, in the order that they are specified. E.g. 
        /// if <see cref="SetOrder"/> gets called with handlers A, C, and E, and <see cref="Filter"/>
        /// gets called with handlers B, C, A, E, D, the result is A, C, E, B, D. This method should be called only once.
        /// </summary>
        public void SetOrder(params Type[] handlerTypes)
        {
            if (orders.Any())
            {
                throw new InvalidOperationException(
                    string.Format(
                        @"Cannot specify the order of handlers twice.

It's not that I don't want to, it's just that I wouldn't know what to make of two
calls - e.g. if a handler pipeline has a handler from each of the calls, how would
these two handlers be ordered?

Please remember that this is just RearrangeHandlersPipelineInspector talking - i.e.
only ONE possible implementation of IInspectHandlerPipeline - so if you feel that
there's something that I don't express clearly enough, or otherwise can't fulfill
your needs, please feel free to implement something more expressive yourself.

Oh, btw. - if you're in doubt as to where the first call to SetOrder happened, here's
the stacktrace:

{0}",
                        stackTrace));
            }

            stackTrace = Environment.StackTrace;

            orders = handlerTypes
                .Select((type, index) => new {Type = type, Index = index})
                .ToDictionary(k => k.Type, v => v.Index);

            SetMaxIndex();
        }

        void SetMaxIndex()
        {
            maxIndex = orders.Any() ? orders.Max(o => o.Value) + 1 : 0;
        }

        internal Type[] GetOrder()
        {
            return orders.Keys.ToArray();
        }

        /// <summary>
        /// Appends the given type to the order currently stored in the pipeline inspector.
        /// This method allows this pipeline inspector to be built incrementally, which
        /// was neede in order to support the fluent configuration API.
        /// </summary>
        public void AddToOrder(Type typeToAppendToOrder)
        {
            if (orders == null)
            {
                orders = new Dictionary<Type, int>();
            }

            if (orders.ContainsKey(typeToAppendToOrder))
            {
                throw new InvalidOperationException(string.Format(@"
Attempted to add {0} to fixed order list of handlers.

Each handler type can only be added once, because that is the only thing that makes
sense, because what would it mean if one particular handler should be first AND last
at the same time?", typeToAppendToOrder));
            }

            orders = orders
                .Concat(new[] {new KeyValuePair<Type, int>(typeToAppendToOrder, maxIndex)})
                .ToDictionary(k => k.Key, v => v.Value);

            SetMaxIndex();
        }
    }
}