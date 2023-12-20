using NUnit.Framework;
using Rebus.Retry.Info;
using Rebus.Tests.Contracts.Errors;

namespace Rebus.Tests.Retry.Info;

[TestFixture]
public class TestToStringExceptionInfo : ExceptionInfoTests<ToStringExceptionInfoFactory>
{
}