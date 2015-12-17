using Rebus.Timeouts;

namespace Rebus.Tests.Contracts.Timeouts
{
    public interface ITimeoutManagerFactory
    {
        ITimeoutManager Create();
        void Cleanup();
        string GetDebugInfo();
    }
}