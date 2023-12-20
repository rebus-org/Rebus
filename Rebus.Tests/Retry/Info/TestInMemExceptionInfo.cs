using System;
using NUnit.Framework;
using Rebus.Retry.Info;
using Rebus.Tests.Contracts.Errors;

namespace Rebus.Tests.Retry.Info;

[TestFixture]
public class TestInMemExceptionInfo : ExceptionInfoTests<InMemExceptionInfoFactory>
{
    [Test]
    public void CreatesInMemExceptionInfo()
    {
        var info = _factory.CreateInfo(new Exception("a"));
        Assert.That(() => info.ConvertTo<InMemExceptionInfo>(), Throws.Nothing);
    }

    [Test]
    public void ExceptionEqualsOriginalException()
    {
        var originalException = new Exception("a");
        var info = _factory.CreateInfo(originalException).ConvertTo<InMemExceptionInfo>();
        Assert.That(info.Exception, Is.SameAs(originalException));
    }
}