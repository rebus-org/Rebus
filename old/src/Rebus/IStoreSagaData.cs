namespace Rebus
{
    /// <summary>
    /// Implement this in order to control how saga data gets persisted.
    /// </summary>
    public interface IStoreSagaData
    {
        /// <summary>
        /// Inserts the specified saga data, ensuring that the specified fields can be used
        /// to correlate with incoming messages. If a saga already exists with the specified
        /// ID and/or correlations, an <see cref="OptimisticLockingException"/> must be thrown.
        /// </summary>
        void Insert(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex);

        /// <summary>
        /// Updates the specified saga data in the underlying data store, ensuring that the
        /// specified fields can be used to correlate with incoming messages. If the saga no
        /// longer exists, or if the revision does not correspond to the expected revision number,
        /// and <see cref="OptimisticLockingException"/> must be thrown.
        /// </summary>
        void Update(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex);

        /// <summary>
        /// Deletes the specified saga data from the underlying data store.
        /// </summary>
        /// <param name="sagaData"></param>
        void Delete(ISagaData sagaData);

        /// <summary>
        /// Queries the underlying data store for the saga whose correlation field has a value
        /// that matches the given field from the incoming message.
        /// </summary>
        T Find<T>(string sagaDataPropertyPath, object fieldFromMessage) where T : class, ISagaData;
    }

    /// <summary>
    /// Implement this on a saga persister if it can handle multiple sagas atomically in one transaction.
    /// </summary>
    public interface ICanUpdateMultipleSagaDatasAtomically
    {
    }

}