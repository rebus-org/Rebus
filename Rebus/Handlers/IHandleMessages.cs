using System.Threading.Tasks;

namespace Rebus.Handlers;

/// <summary>
/// Base message handler interface. Don't implement this one directly, it would give you nothing
/// </summary>
public interface IHandleMessages { }
    
/// <summary>
/// Message handler interface. Implement this in order to get to handle messages of a specific type
/// </summary>
public interface IHandleMessages<in TMessage> : IHandleMessages
{
    /// <summary>
    /// This method will be invoked with a message of type <typeparamref name="TMessage"/>
    /// </summary>
    Task Handle(TMessage message);
}