namespace Rebus.Routing.TransportMessages;

enum ActionType
{
    /// <summary>
    /// Doesn't do anything - dispatches the message as normally
    /// </summary>
    None,

    /// <summary>
    /// Forwards the message to one or more recipients
    /// </summary>
    Forward,

    /// <summary>
    /// Ignores the message (thus effectively losing it)
    /// </summary>
    Ignore,
}