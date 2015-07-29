using System;
using System.Collections.Concurrent;

namespace Rebus
{
    /// <summary>
    /// Saga base class that allows for passing around non-generic references to saga handlers
    /// </summary>
    public abstract class Saga
    {
        /// <summary>
        /// Sets up the internal dictionary of correlations in the saga
        /// </summary>
        protected Saga()
        {
            Correlations = new ConcurrentDictionary<Type, Correlation>();
        }

        internal ConcurrentDictionary<Type, Correlation> Correlations { get; set; }
        
        internal bool Complete { get; set; }
        
        /// <summary>
        /// Indicates whether the saga data instance mounted on this saga handler is new (i.e. it is not yet persistent)
        /// </summary>
        public bool IsNew { get; internal set; }
        
        /// <summary>
        /// Should fill the internally stored dictionary of correlations by invoking the nifty
        /// <code>Incoming&lt;TMessage&gt;(m => m.MessageProperty).CorrelatesWith(d => d.SagaDataProperty);</code>
        /// syntax.
        /// </summary>
        public abstract void ConfigureHowToFindSaga();
    }

    /// <summary>
    /// Extends <see cref="Saga"/> with type information that specifies which kind of saga data this saga uses to represent its state
    /// </summary>
    public abstract class Saga<TData> : Saga where TData : ISagaData
    {
        /// <summary>
        /// Gives access to the current saga data instance
        /// </summary>
        public TData Data { get; internal set; }

        /// <summary>
        /// Starts building a correlation expression
        /// </summary>
        protected Correlator<TData, TMessage> Incoming<TMessage>(Func<TMessage, object> messageProperty) where TMessage : class
        {
            return new Correlator<TData, TMessage>(messageProperty, this);
        }

        /// <summary>
        /// Marks the saga as complete, which results in the saga data effectively being deleted
        /// </summary>
        protected void MarkAsComplete()
        {
            Complete = true;
        }
    }
}