using System;
using System.Linq;
using NUnit.Framework;
using Rebus.Tests.Persistence;
using Rebus.Tests.Transports.Rabbit;

namespace Rebus.Tests
{
    [TestFixture]
    public class TestCategories
    {
        public const string Rabbit = "rabbit";
        public const string Integration = "integration";
        public const string Mongo = "mongo";
        public const string Raven = "raven";
        public const string MsSql = "mssql";
        public const string ToDo = "todo";
        public const string Performance = "performance";

        public const bool IgnoreLongRunningTests = true;

        [TestCase(typeof(MongoDbFixtureBase), Mongo)]
        [TestCase(typeof(SqlServerFixtureBase), MsSql)]
        [TestCase(typeof(RabbitMqFixtureBase), Rabbit)]
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