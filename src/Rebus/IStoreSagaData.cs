namespace Rebus
{
    /// <summary>
    /// Implement this in order to control how saga data gets persisted
    /// </summary>
    public interface IStoreSagaData
    {
        /// <summary>
        /// todo: comment
        /// </summary>
        void Insert(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex);
        
        void Update(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex);
        void Delete(ISagaData sagaData);
        T Find<T>(string sagaDataPropertyPath, object fieldFromMessage) where T : ISagaData;
    }
}