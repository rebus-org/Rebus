using System;
using Rebus.Messages;

namespace Rebus.Auditing.Messages;

/// <summary>
/// Headers relevant when using message auditing
/// </summary>
public static class AuditHeaders
{
    /// <summary>
    /// Header with <see cref="DateTimeOffset"/> (serialized with the format string 'O') of the time when the message copy was forwarded to the audit queue.
    /// </summary>
    public const string AuditTime = "rbs2-audit-copytime";

    /// <summary>
    /// Header with <see cref="DateTimeOffset"/> (serialized with the format string 'O') of the time when the audited message was received.
    /// Only present on copies of handled messages (i.e. not on messages with <see cref="Headers.IntentOptions.PublishSubscribe"/>
    /// </summary>
    public const string HandleTime = "rbs2-audit-handletime";
        
    /// <summary>
    /// Header with the queue name from which the message was received. 
    /// Only present on copies of handled messages (i.e. not on messages with <see cref="Headers.IntentOptions.PublishSubscribe"/>
    /// </summary>
    public const string HandleQueue = "rbs2-audit-handlequeue";
        
    /// <summary>
    /// Name of the machine that processed/published the message
    /// </summary>
    public const string MachineName = "rbs2-audit-machine";
}