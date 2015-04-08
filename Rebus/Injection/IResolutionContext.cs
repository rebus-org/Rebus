namespace Rebus.Injection
{
    /// <summary>
    /// Represents the context of resolving one root service and can be used throughout the tree to fetch something to be injected
    /// </summary>
    public interface IResolutionContext
    {
        TService Get<TService>();
        void DisposeTrackedInstances();
    }
}