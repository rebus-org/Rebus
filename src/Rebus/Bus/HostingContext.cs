using System;
using System.Collections;
using System.Collections.Generic;
using System.ServiceModel;
using System.Web;

namespace Rebus.Bus
{
    /// <summary>
    /// An abstraction for HttpContext and OperationContext
    /// </summary>
    public interface IContext : IDisposable
    {
        /// <summary>
        /// The collection of objects i context
        /// </summary>
        IDictionary Items { get; }

        /// <summary>
        /// Indicating whether this context is present
        /// </summary>
        bool InContext { get; }
    }

    internal class RebusHttpContext : IContext
    {
        const string RebusHttpTransactionContextKey = "RebusHttpTransactionContext";

        public IDictionary Items
        {
            get
            {
                if(HttpContext.Current == null)
                    return null;

                var items = HttpContext.Current.Items[RebusHttpTransactionContextKey] as IDictionary;
                if (items == null)
                {
                    items = new Dictionary<string, object>();
                    HttpContext.Current.Items[RebusHttpTransactionContextKey] = items;
                }

                return items;
            }
        }

        public bool InContext
        {
            get { return HttpContext.Current != null; }
        }

        public void Dispose()
        {
            if (HttpContext.Current != null)
            {
                HttpContext.Current.Items.Remove(RebusHttpTransactionContextKey);
            }
        }
    }

    internal class RebusOperationContext : IContext
    {
        public IDictionary Items
        {
            get
            {
                return WcfOperationContext.Current == null ? null : WcfOperationContext.Current.Items;
            }
        }

        public bool InContext
        {
            get { return WcfOperationContext.Current != null; }
        }

        public void Dispose()
        {
            WcfOperationContext.Clear();
        }
    }

    // http://stackoverflow.com/questions/1895732/where-to-store-data-for-current-wcf-call-is-threadstatic-safe
    internal class WcfOperationContext : IExtension<OperationContext>
    {
        private readonly IDictionary items;

        private WcfOperationContext()
        {
            items = new Dictionary<string, object>();
        }

        public IDictionary Items
        {
            get { return items; }
        }

        public static WcfOperationContext Current
        {
            get
            {
                if (OperationContext.Current == null)
                    return null;

                var context = OperationContext.Current.Extensions.Find<WcfOperationContext>();
                if (context == null)
                {
                    context = new WcfOperationContext();
                    OperationContext.Current.Extensions.Add(context);
                }
                return context;
            }
        }

        public void Attach(OperationContext owner) { }
        public void Detach(OperationContext owner) { }

        public static void Clear()
        {
            if (OperationContext.Current != null)
            {
                var context = OperationContext.Current.Extensions.Find<WcfOperationContext>();
                if (context != null)
                {
                    OperationContext.Current.Extensions.Remove(context);
                }
            }
        }
    }
}