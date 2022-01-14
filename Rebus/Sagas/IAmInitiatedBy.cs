using Rebus.Handlers;

namespace Rebus.Sagas;

/// <summary>
/// Derived marker interface, allowing for a handler to indicate that messages of type <typeparamref name="TMessage"/> 
/// are allowed to instantiate new saga instances if the message cannot be correlated with an already existing instance
/// </summary>
public interface IAmInitiatedBy<in TMessage> : IHandleMessages<TMessage> { }