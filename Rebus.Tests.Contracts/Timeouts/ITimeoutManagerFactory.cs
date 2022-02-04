using System;
using Rebus.Timeouts;

namespace Rebus.Tests.Contracts.Timeouts;

public interface ITimeoutManagerFactory
{
    ITimeoutManager Create();
    void Cleanup();
    string GetDebugInfo();
    void FakeIt(DateTimeOffset fakeTime);
}