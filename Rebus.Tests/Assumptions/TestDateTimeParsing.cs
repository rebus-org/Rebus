using System;
using NUnit.Framework;

namespace Rebus.Tests.Assumptions;

[TestFixture]
public class TestDateTimeParsing
{
    [Test]
    public void CheckIt()
    {
        var withoutOffset = DateTimeOffset.Parse("2015-12-31T23:00:00Z");
        var withOffset = DateTimeOffset.Parse("2016-01-01T00:00:00.0000000+01:00");

        Assert.That(withOffset, Is.EqualTo(withoutOffset));
    }
}