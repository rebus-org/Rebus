using MongoDB.Driver;

namespace Rebus.Tests.Persistence
{
    public class MongoHelper
    {
        public static MongoDatabase GetDatabase(string connectionString)
        {
            var mongoUrl = new MongoUrl(connectionString);
            
            return new MongoClient(mongoUrl)
                .GetServer()
                .GetDatabase(mongoUrl.DatabaseName);
        }
    }
}