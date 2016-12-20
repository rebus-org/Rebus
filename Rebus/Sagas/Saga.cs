using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Rebus.Exceptions;
using Rebus.Reflection;
#pragma warning disable 1998

namespace Rebus.Sagas
{
    /// <summary>
    /// Saga base class that allows for passing around saga instances without being bothered by the type of saga data they're handling. You should
    /// probably not inherit from this one, inherit your saga from <see cref="Saga{TSagaData}"/> instead.
    /// </summary>
    public abstract class Saga
    {
        static readonly ConcurrentDictionary<Type, bool> CachedUserHasOverriddenConflictResolutionMethod = new ConcurrentDictionary<Type, bool>();

        /// <summary>
        /// Checks whether the <see cref="Saga{TSagaData}.ResolveConflict"/> method is defined in <see cref="Saga{TSagaData}"/>, returning
        /// true if it is NOT - because that means that the user has overridden the method and in this particular saga type can resolve conflicts. 
        /// </summary>
        internal bool UserHasOverriddenConflictResolutionMethod()
        {
            return CachedUserHasOverriddenConflictResolutionMethod
                .GetOrAdd(GetType(), type =>
                {
                    var typeDeclaringTheConflictResolutionMethod = GetType().GetTypeInfo()
                        .GetMethod("ResolveConflict", BindingFlags.Instance | BindingFlags.NonPublic).DeclaringType;

                    if (typeDeclaringTheConflictResolutionMethod == null)
                    {
                        return false;
                    }

                    return !(typeDeclaringTheConflictResolutionMethod.GetTypeInfo().IsGenericType
                             && typeDeclaringTheConflictResolutionMethod.GetGenericTypeDefinition() == typeof (Saga<>));
                });
        }

        internal IEnumerable<CorrelationProperty> GetCorrelationProperties()
        {
            return new List<CorrelationProperty>();
        }

        internal abstract IEnumerable<CorrelationProperty> GenerateCorrelationProperties();

        internal abstract Type GetSagaDataType();
        
        internal abstract ISagaData CreateNewSagaData();

        internal bool WasMarkedAsComplete { get; set; }

        internal bool WasMarkedAsUnchanged { get; set; }

        internal bool HoldsNewSagaDataInstance { get; set; }

        /// <summary>
        /// Marks the current saga instance as completed, which means that it is either a) deleted from persistent storage in case
        /// it has been made persistent, or b) thrown out the window if it was never persisted in the first place.
        /// </summary>
        protected virtual void MarkAsComplete()
        {
            WasMarkedAsComplete = true;
        }

        /// <summary>
        /// Marks the current saga instance as unchanged, causing any changes made to it to be ignored. Its revision will NOT be
        /// incremented
        /// </summary>
        protected virtual void MarkAsUnchanged()
        {
            WasMarkedAsUnchanged = true;
        }

        /// <summary>
        /// Gets whether the saga data instance is new
        /// </summary>
        protected bool IsNew => HoldsNewSagaDataInstance;

        internal abstract Task InvokeConflictResolution(ISagaData otherSagaData);
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

        internal sealed override IEnumerable<CorrelationProperty> GenerateCorrelationProperties()
        {
            var configuration = new CorrelationConfiguration(GetType());
            
            CorrelateMessages(configuration);
            
            return configuration.GetCorrelationProperties();
        }

        internal sealed override async Task InvokeConflictResolution(ISagaData otherSagaData)
        {
            await ResolveConflict((TSagaData) otherSagaData);
        }

        /// <summary>
        /// Override this to be given an opportunity to resolve the conflict when a <see cref="ConcurrencyException"/> occurs on an update.
        /// If a conflict cannot be resolved, feel free to bail out by throwing an exception.
        /// </summary>
        protected virtual async Task ResolveConflict(TSagaData otherSagaData)
        {
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
                        throw new RebusApplicationException(exception, $"Could not extract correlation value from message {typeof (TMessage)}");
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
            var newSagaData = new TSagaData
            {
                Id = Guid.NewGuid()
            };

            HoldsNewSagaDataInstance = true;

            return newSagaData;
        }
    }
}