using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Retry;

namespace Rebus.Tests.Contracts.Errors;

public class ExceptionInfoTests<TExceptionInfoFactory> : FixtureBase where TExceptionInfoFactory : IExceptionInfoFactory, new()
{
    protected TExceptionInfoFactory _factory;

    protected override void SetUp()
    {
        base.SetUp();
        
        _factory = new TExceptionInfoFactory();
    }

    [Test]
    public async Task ThrowsOnNullException()
    {
        Assert.Throws<ArgumentNullException>((Action)(() => _factory.CreateInfo(null)));
    }

    [Test]
    public async Task TypeEqualsQualifiedExceptionType()
    {
        var info = _factory.CreateInfo(new Exception("a"));
        Assert.That(info.Type, Is.EqualTo("System.Exception, mscorlib"));
    }

    [Test]
    public async Task MessageIncludesExceptionMessage()
    {
        var info = _factory.CreateInfo(new Exception("a"));
        Assert.That(info.Message, Contains.Substring("a"));
    }

    [Test]
    public async Task DetailsIsNotEmpty()
    {
        var info = _factory.CreateInfo(new Exception("a"));
        Assert.That(info.Details, Is.Not.Empty);
    }

    [Test]
    public async Task TimeIsRoughlyNow()
    {
        var info = _factory.CreateInfo(new Exception("a"));
        Assert.That(info.Time, Is.EqualTo(DateTimeOffset.Now).Within(TimeSpan.FromSeconds(1)));
    }

    [Test]
    public async Task GetFullErrorDescriptionIsNotEmpty()
    {
        var info = _factory.CreateInfo(new Exception("a"));
        Assert.That(info.GetFullErrorDescription(), Is.Not.Empty);
    }

    [Test]
    public async Task ConvertToThrowsOnUnexpectedInfoType()
    {
        var info = _factory.CreateInfo(new Exception("a"));
        Assert.Throws<ArgumentException>((Action)(() => info.ConvertTo<UnexpectedExceptionInfo>()));
    }

    record UnexpectedExceptionInfo : ExceptionInfo
    {
        public UnexpectedExceptionInfo(string Type, string Message, string Details, DateTimeOffset Time) : base(Type, Message, Details, Time) { }
    }
}