using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Rebus2.Handlers;
using Rebus2.Reflection;

namespace Rebus2.Sagas
{
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

    public abstract class Saga<TSagaData> : Saga where TSagaData : ISagaData, new()
    {
        public TSagaData Data { get; set; }

        protected abstract void CorrelateMessages(ICorrelationConfig<TSagaData> config);

        internal override IEnumerable<CorrelationProperty> GenerateCorrelationProperties()
        {
            var configuration = new CorrelationConfiguration();
            
            CorrelateMessages(configuration);
            
            return configuration.GetCorrelationProperties();
        }

        class CorrelationConfiguration : ICorrelationConfig<TSagaData>
        {
            readonly List<CorrelationProperty> _correlationProperties = new List<CorrelationProperty>();
            
            public void Correlate<TMessage>(Func<TMessage, object> messageValueExtractor, Expression<Func<TSagaData, object>> sagaDataValueExpression)
            {
                var propertyName = Reflect.Path(sagaDataValueExpression);
                
                Func<object, object> neutralMessageValueExtractor = message =>
                {
                    try
                    {
                        return messageValueExtractor((TMessage) message);
                    }
                    catch (Exception exception)
                    {
                        throw new ApplicationException(string.Format("Could not extract correlation value from message {0}", typeof(TMessage)), exception);
                    }
                };

                _correlationProperties.Add(new CorrelationProperty(typeof(TMessage), neutralMessageValueExtractor, typeof(TSagaData), propertyName));
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

    public interface IAmInitiatedBy<T> : IHandleMessages<T> { }

    public interface ISagaData
    {
        Guid Id { get; set; }
    }
}