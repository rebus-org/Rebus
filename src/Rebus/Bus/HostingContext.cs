using System;
using System.Collections;
using System.Collections.Generic;
using System.ServiceModel;
using System.Web;

namespace Rebus.Bus
{
    internal static class RebusHttpContext 
    {
        const string RebusTransactionContextKey = "RebusTransactionContext";
        const string RebusHttpContextKey = "RebusHttpContext";

        public static bool InContext
        {
            get { return HttpContext.Current != null; }
        }

        public static ITransactionContext TransactionContext
        {
            get
            {
                if (!InContext)
                    throw new InvalidOperationException("Trying to access HttpContext in a non-HttpContext");
                return Items[RebusTransactionContextKey] as ITransactionContext;
            }
            set
            {
                if (!InContext)
                    throw new InvalidOperationException("Trying to access HttpContext in a non-HttpContext");
                Items[RebusTransactionContextKey] = value;
            }
        }

        public static void Clear()
        {
            if (HttpContext.Current != null)
            {
                HttpContext.Current.Items.Remove(RebusHttpContextKey);
            }
        }

        private static IDictionary Items
        {
            get
            {
                if (HttpContext.Current == null)
                    return null;

                var items = HttpContext.Current.Items[RebusHttpContextKey] as IDictionary;
                if (items == null)
                {
                    items = new Dictionary<string, object>();
                    HttpContext.Current.Items[RebusHttpContextKey] = items;
                }

                return items;
            }
        }
    }

    internal static class RebusOperationContext
    {
        const string RebusTransactionContextKey = "RebusTransactionContext";

        public static ITransactionContext TransactionContext
        {
            get
            {
                if (!InContext)
                    throw new InvalidOperationException("Trying to access OperationContext in a non-OperationContext"); 
                return WcfOperationContext.Current.Items[RebusTransactionContextKey] as ITransactionContext;
            }
            set
            {
                if (!InContext)
                    throw new InvalidOperationException("Trying to access OperationContext in a non-OperationContext");
                WcfOperationContext.Current.Items[RebusTransactionContextKey] = value;
            }
        }

        public static bool InContext
        {
            get { return WcfOperationContext.Current != null; }
        }

        public static void Clear()
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
            if (OperationContext.Current == null) return;

            var context = OperationContext.Current.Extensions.Find<WcfOperationContext>();
            if (context != null)
            {
                OperationContext.Current.Extensions.Remove(context);
            }
        }
    }
}