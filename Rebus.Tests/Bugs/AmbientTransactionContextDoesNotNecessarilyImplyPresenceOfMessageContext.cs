using NUnit.Framework;
using Rebus.Pipeline;
using Rebus.Tests.Contracts;
using Rebus.Transport;

#pragma warning disable 1998

namespace Rebus.Tests.Bugs;

[TestFixture]
public class AmbientTransactionContextDoesNotNecessarilyImplyPresenceOfMessageContext : FixtureBase
{
    [Test]
    public void ItsTrueItDoesnt()
    {
        // create an ambient transaction context
        using (new RebusTransactionScope())
        {
            Assert.That(AmbientTransactionContext.Current, Is.Not.Null, 
                "Expected an ambient transaction context to be present in here");

            Assert.That(MessageContext.Current, Is.Null, 
                "Did NOT expect a message context to be here, because we're not currently receiving anything!!");
        }
    }
}