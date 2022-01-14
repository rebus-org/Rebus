using NUnit.Framework;
using Rebus.Extensions;
using Rebus.Topic;

namespace Rebus.Tests.Topic;

[TestFixture]
public class TestDefaultTopicNameConvention
{
    [Test]
    public void DefaultTopicNameConventionUseGetAssExtension()
    {
        var convention = new DefaultTopicNameConvention();

        var expected = typeof(SimpleMessage).GetSimpleAssemblyQualifiedName();
        var actual = convention.GetTopic(typeof(SimpleMessage));

        Assert.That(actual, Is.EqualTo(expected));
    }
}

public class SimpleMessage
{
    public string Something { get; set; }

}