using Rebus.Handlers;

namespace Rebus.Sagas
{
    public interface IAmInitiatedBy<T> : IHandleMessages<T> { }
}