using System;
using System.Collections.Generic;
using System.Linq.Expressions;
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
    }

    /// <summary>
    /// Generic saga base class that must be made concrete by supplying the <see cref="TSagaData"/> type parameter.
    /// </summary>
    public abstract class Saga<TSagaData> : Saga where TSagaData : ISagaData, new()
    {
        public TSagaData Data { get; set; }

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
                        throw new ApplicationException(string.Format("Could not extract correlation value from message {0}", typeof(TMessage)), exception);
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