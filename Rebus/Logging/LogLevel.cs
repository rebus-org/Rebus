namespace Rebus.Logging;

/// <summary>
/// Enumeration of the log levels available with Rebus loggers. As a general rule of thumb, levels WARN
/// and ERROR should, as a minimum, ALWAYS be logged to a local file or another persistent means.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Log statement of very low importance which is most likely only relevant in extreme debugging scenarios
    /// </summary>
    Debug = 0,
        
    /// <summary>
    /// Log statement of low importance to unwatched running systems which however can be very relevant when testing and debugging
    /// </summary>
    Info = 1,

    /// <summary>
    /// Log statement of fairly high importance - always contains relevant information on somewhing that may be a sign that something is wrong
    /// </summary>
    Warn = 2,
        
    /// <summary>
    /// Log statement of the highest importance - always contains relevant information on something that has gone wrong
    /// </summary>
    Error = 3,
}