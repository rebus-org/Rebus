using NUnit.Framework;
using Rebus.Tests.Contracts.Subscriptions;

namespace Rebus.Tests.Persistence.Filesystem;

[TestFixture]
public class JsonFileSubscriptionStorageBasicSubscriptionOperations : BasicSubscriptionOperations<JsonFileSubscriptionStorageFactory>
{
}