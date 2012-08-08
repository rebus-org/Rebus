namespace Rebus.Tests.Contracts.ContainerAdapters
{
    public interface IContainerAdapterFactory
    {
        IContainerAdapter Create();
        void DisposeInnerContainer();
    }
}