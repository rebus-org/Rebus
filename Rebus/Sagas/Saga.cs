using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Rebus.Exceptions;
using Rebus.Reflection;

namespace Rebus.Sagas
{
    /// <summary>
    /// Saga base class that allows for passing around saga instances without being bothered by the type of saga data they're handling. You should
    /// probably not inherit from this one, inherit your saga from <see cref="Saga{TSagaData}"/> instead.
    /// </summary>
    public abstract class Saga
    {
        internal IEnumerable<CorrelationProperty> GetCorrelationProperties()
        {
            return new List<CorrelationProperty>();
        }

        internal abstract IEnumerable<CorrelationProperty> GenerateCorrelationProperties();

        internal abstract Type GetSagaDataType();
        
        internal abstract ISagaData CreateNewSagaData();

        internal bool WasMarkedAsComplete { get; set; }

        /// <summary>
        /// Marks the current saga instance as completed, which means that it is either a) deleted from persistent storage in case
        /// it has been made persistent, or b) thrown out the window if it was never persisted in the first place.
        /// </summary>
        protected virtual void MarkAsComplete()
        {
            WasMarkedAsComplete = true;
        }
    }

    /// <summary>
    /// Generic saga base class that must be made concrete by supplying the <typeparamref name="TSagaData"/> type parameter.
    /// </summary>
    public abstract class Saga<TSagaData> : Saga where TSagaData : ISagaData, new()
    {
        /// <summary>
        /// Gets or sets the relevant saga data instance for this saga handler
        /// </summary>
        public TSagaData Data { get; set; }

        /// <summary>
        /// This method must be implemented in order to configure correlation of incoming messages with existing saga data instances.
        /// Use the injected <see cref="ICorrelationConfig{TSagaData}"/> to set up the correlations, e.g. like so:
        /// <code>
        /// config.Correlate&lt;InitiatingMessage&gt;(m => m.OrderId, d => d.CorrelationId);
        /// config.Correlate&lt;CorrelatedMessage&gt;(m => m.CorrelationId, d => d.CorrelationId);
        /// </code>
        /// </summary>
        protected abstract void CorrelateMessages(ICorrelationConfig<TSagaData> config);

        internal override IEnumerable<CorrelationProperty> GenerateCorrelationProperties()
        {
            var configuration = new CorrelationConfiguration(GetType());
            
            CorrelateMessages(configuration);
            
            return configuration.GetCorrelationProperties();
        }

        class CorrelationConfiguration : ICorrelationConfig<TSagaData>
        {
            readonly Type _sagaType;

            public CorrelationConfiguration(Type sagaType)
            {
                _sagaType = sagaType;
            }

            readonly List<CorrelationProperty> _correlationProperties = new List<CorrelationProperty>();
            
            public void Correlate<TMessage>(Func<TMessage, object> messageValueExtractorFunction, Expression<Func<TSagaData, object>> sagaDataValueExpression)
            {
                var propertyName = Reflect.Path(sagaDataValueExpression);
                
                Func<object, object> neutralMessageValueExtractor = message =>
                {
                    try
                    {
                        return messageValueExtractorFunction((TMessage) message);
                    }
                    catch (Exception exception)
                    {
                        throw new RebusApplicationException(string.Format("Could not extract correlation value from message {0}", typeof(TMessage)), exception);
                    }
                };

                _correlationProperties.Add(new CorrelationProperty(typeof(TMessage), neutralMessageValueExtractor, typeof(TSagaData), propertyName, _sagaType));
            }

            public IEnumerable<CorrelationProperty> GetCorrelationProperties()
            {
                return _correlationProperties;
            }
        }

        internal override Type GetSagaDataType()
        {
            return typeof (TSagaData);
        }

        internal override ISagaData CreateNewSagaData()
        {
            return new TSagaData
            {
                Id = Guid.NewGuid()
            };
        }
    }
}