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
using NUnit.Framework;
using Rebus.Tests.Persistence.MongoDb;
using Rebus.Tests.Persistence.SqlServer;

namespace Rebus.Tests
{
    [TestFixture]
    public class TestCategories
    {
        public const string Integration = "integration";
        public const string Mongo = "mongo";
        public const string MsSql = "mssql";
        public const string ToDo = "todo";
        public const string Performance = "performance";

        [TestCase(typeof(MongoDbFixtureBase), Mongo)]
        [TestCase(typeof(DbFixtureBase), MsSql)]
        public void AssertCategoriesIfPossible(Type fixtureBaseType, string expectedCategoryName)
        {
            GetType().Assembly.GetTypes()
                .Where(t => fixtureBaseType.IsAssignableFrom(t)
                            && t.GetCustomAttributes(typeof(TestFixtureAttribute), false).Any())
                .ToList().ForEach(t => AssertCategory(t, expectedCategoryName, fixtureBaseType));
        }

        void AssertCategory(Type testFixtureType, string expectedCategory, Type fixtureBaseType)
        {
            var categoryAttributes = testFixtureType
                .GetCustomAttributes(typeof(CategoryAttribute), false)
                .Cast<CategoryAttribute>();

            if (!categoryAttributes.Any(a => a.Name == expectedCategory))
            {
                Assert.Fail(
                    @"Could not find category attribute with name '{0}' on {1}.

Test fixtures derived from {2} shoule be decorated with Category(""{0}"").",
                    expectedCategory,
                    testFixtureType,
                    fixtureBaseType);
            }
        }
    }
}