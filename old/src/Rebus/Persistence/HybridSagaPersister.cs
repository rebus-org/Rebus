using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Persistence
{
    /// <summary>
    /// Experimental saga persister that is capable of using different saga persisters depending on the type of saga data.
    /// </summary>
    public class HybridSagaPersister : IStoreSagaData
    {
        /// <summary>
        /// Stores the fallback saga persister which will be used for all kinds of saga data when a custom persister has not been configured
        /// </summary>
        readonly IStoreSagaData fallbackSagaPersister;

        /// <summary>
        /// Contains a list of available persister instances. To actually use one of these, <see cref="Customize{TSagaData,TSagaPersister}"/> must
        /// be called in order to map a specific saga data type to a persister type.
        /// </summary>
        readonly List<IStoreSagaData> availableCustomSagaPersisters = new List<IStoreSagaData>();

        /// <summary>
        /// Contains the actual (sagaDataType) => (persisterInstance) mapping.
        /// </summary>
        readonly ConcurrentDictionary<Type, IStoreSagaData> customSagaPersisters = new ConcurrentDictionary<Type, IStoreSagaData>();

        /// <summary>
        /// Constructs the hybrid saga persister and configures the fallback saga persister, which will be used in all cases
        /// where a custom saga persister has not been supplied.
        /// </summary>
        public HybridSagaPersister(IStoreSagaData fallbackSagaPersister)
        {
            if (fallbackSagaPersister == null)
            {
                throw new ArgumentException(@"When configuring the HybridSagapersister, it is important that you supply a fallback saga persister which will be used in cases where no customized saga persister can be found.");
            }

            this.fallbackSagaPersister = fallbackSagaPersister;
        }

        /// <summary>
        /// Inserts the specified saga data, ensuring that the specified fields can be used to correlate with incoming messages. If a saga already exists with the specified
        /// ID and/or correlations, an <see cref="OptimisticLockingException"/> must be thrown. If configured, a custom saga persister will be used to actually carry out the operation,
        /// otherwise the fallback saga persister will be used.
        /// </summary>
        public void Insert(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
        {
            (GetCustomPersisterOrNull(sagaData.GetType()) ?? fallbackSagaPersister).Insert(sagaData, sagaDataPropertyPathsToIndex);
        }

        /// <summary>
        /// Updates the specified saga data in the underlying data store, ensuring that the specified fields can be used to correlate with incoming messages. If the saga no
        /// longer exists, or if the revision does not correspond to the expected revision number, and <see cref="OptimisticLockingException"/> must be thrown. If configured, 
        /// a custom saga persister will be used to actually carry out the operation, otherwise the fallback saga persister will be used.
        /// </summary>
        public void Update(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
        {
            (GetCustomPersisterOrNull(sagaData.GetType()) ?? fallbackSagaPersister).Update(sagaData, sagaDataPropertyPathsToIndex);
        }

        /// <summary>
        /// Deletes the specified saga data from the underlying data store. If configured, a custom saga persister will be used to actually carry out the operation,
        /// otherwise the fallback saga persister will be used.
        /// </summary>
        public void Delete(ISagaData sagaData)
        {
            (GetCustomPersisterOrNull(sagaData.GetType()) ?? fallbackSagaPersister).Delete(sagaData);
        }

        /// <summary>
        /// Queries the underlying data store for the saga whose correlation field has a value that matches the given field from the incoming message.
        /// If configured, a custom saga persister will be used to actually carry out the operation, otherwise the fallback saga persister will be used.
        /// </summary>
        public TSagaData Find<TSagaData>(string sagaDataPropertyPath, object fieldFromMessage) where TSagaData : class, ISagaData
        {
            return (GetCustomPersisterOrNull(typeof(TSagaData)) ?? fallbackSagaPersister).Find<TSagaData>(sagaDataPropertyPath, fieldFromMessage);
        }

        /// <summary>
        /// Adds the specified saga persister to the list of available saga persisters. At most one persister of each type may be added this way.
        /// </summary>
        public HybridSagaPersister Add(IStoreSagaData customSagaPersister)
        {
            availableCustomSagaPersisters.Add(customSagaPersister);
            return this;
        }

        /// <summary>
        /// Specifies for the given <typeparamref name="TSagaData"/> type that the persister of type <typeparamref name="TSagaPersister"/> must be used.
        /// An instance of <typeparamref name="TSagaPersister"/> must be available at this point, so it must be added by calling <see cref="Add"/> before
        /// calling this method.
        /// </summary>
        public HybridSagaPersister Customize<TSagaData, TSagaPersister>()
            where TSagaData : ISagaData
            where TSagaPersister : IStoreSagaData
        {
            var customPersister = availableCustomSagaPersisters.FirstOrDefault(a => a.GetType() == typeof(TSagaPersister));

            if (customPersister == null)
            {
                throw new InvalidOperationException(string.Format("Could not find an available persister of type {0} - please make one available by calling Add with an instance before calling this method",
                    typeof(TSagaPersister)));
            }

            Customize(customPersister, typeof (TSagaData));
            
            return this;
        }

        /// <summary>
        /// Specifies for the given <typeparamref name="TSagaData"/> type that the given saga persister instance must be used.
        /// </summary>
        public HybridSagaPersister Customize<TSagaData>(IStoreSagaData customSagaPersisterInstance)
            where TSagaData : ISagaData
        {
            Customize(customSagaPersisterInstance, typeof(TSagaData));

            return this;
        }

        void Customize(IStoreSagaData customSagaPersisterInstance, Type sagaDataType)
        {
            if (customSagaPersisterInstance == null)
            {
                throw new ArgumentNullException("customSagaPersisterInstance", "When adding a custom saga persister, you need to supply an instance!");
            }

            var key = sagaDataType;

            if (customSagaPersisters.ContainsKey(key))
            {
                throw new InvalidOperationException(
                    string.Format(
                        "A custom saga persister of type {0} has already been supplied for {1} - only one custom persister can be supplied for each saga data type!",
                        customSagaPersisters[key].GetType(), key));
            }

            customSagaPersisters[key] = customSagaPersisterInstance;
        }

        IStoreSagaData GetCustomPersisterOrNull(Type type)
        {
            IStoreSagaData customPersister;

            return customSagaPersisters.TryGetValue(type, out customPersister)
                       ? customPersister
                       : null;
        }
    }
}