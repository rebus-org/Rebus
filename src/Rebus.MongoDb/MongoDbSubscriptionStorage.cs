// Copyright 2011 Mogens Heller Grabe
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software distributed under the License is
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.
using System;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Rebus.MongoDb
{
    public class MongoDbSubscriptionStorage : IStoreSubscriptions
    {
        readonly string collectionName;
        readonly MongoDatabase database;

        public MongoDbSubscriptionStorage(string connectionString, string collectionName)
        {
            this.collectionName = collectionName;

            database = MongoDatabase.Create(connectionString);
        }

        public void Store(Type messageType, string subscriberInputQueue)
        {
            var collection = database.GetCollection(collectionName);

            var criteria = Query.EQ("_id", messageType.FullName);
            var update = Update.AddToSet("endpoints", subscriberInputQueue);

            var safeModeResult = collection.Update(criteria, update, UpdateFlags.Upsert, SafeMode.True);

            EnsureResultIsGood(safeModeResult);
        }

        public void Remove(Type messageType, string subscriberInputQueue)
        {
            var collection = database.GetCollection(collectionName);

            var criteria = Query.EQ("_id", messageType.FullName);
            var update = Update.Pull("endpoints", subscriberInputQueue);

            var safeModeResult = collection.Update(criteria, update, UpdateFlags.Upsert, SafeMode.True);

            EnsureResultIsGood(safeModeResult);
        }

        public string[] GetSubscribers(Type messageType)
        {
            var collection = database.GetCollection(collectionName);

            var doc = collection.FindOne(Query.EQ("_id", messageType.FullName)).AsBsonDocument;
            if (doc == null) return new string[0];

            var endpoints = doc["endpoints"].AsBsonArray;
            return endpoints.Values.Select(v => v.ToString()).ToArray();
        }

        void EnsureResultIsGood(SafeModeResult safeModeResult)
        {
                
        }
    }
}