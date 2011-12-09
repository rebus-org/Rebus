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
using System.Collections.Generic;
using MongoDB.Driver;
using NUnit.Framework;
using log4net.Config;

namespace Rebus.Tests.Persistence.MongoDb
{
    public abstract class MongoDbFixtureBase
    {
        static MongoDbFixtureBase()
        {
            XmlConfigurator.Configure();
        }

        protected const string ConnectionString = "mongodb://localhost:27017/rebus_test";
        
        readonly HashSet<string> collectionsToDrop = new HashSet<string>();

        MongoDatabase db;

        [SetUp]
        public void SetUp()
        {
            DoSetUp();

            db = MongoDatabase.Create(ConnectionString);
            collectionsToDrop.Clear();
        }

        protected virtual void DoSetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
            DoTearDown();
        }

        protected virtual void DoTearDown()
        {
        }

        protected void DropCollection(string collectionName)
        {
            db.DropCollection(collectionName);
        }

        protected MongoCollection<T> Collection<T>(string collectionName)
        {
            collectionsToDrop.Add(collectionName);

            return db.GetCollection<T>(collectionName);
        }
    }
}