using NUnit.Framework;
using Rebus.Tests.Contracts.Errors;

namespace Rebus.Tests.Retry.ErrorTracking;

[TestFixture]
public class TestInMemErrorTracker : ErrorTrackerTests<InMemErrorTrackerFactory>
{
}