using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace Rebus.MongoDb
{
    public static class MongoExtensions
    {
        //public static async Task<TDocument> FindFirstAsync<TDocument>(this IMongoCollection<TDocument> collection, Expression<Func<TDocument, bool>> criteria)
        //{
        //    var options = new FindOptions<TDocument> { Limit = 1 };
        //    var results = await collection.FindAsync(criteria, options);
        //    var list = await results.ToListAsync();
        //    return list.FirstOrDefault();
        //}
    }
}