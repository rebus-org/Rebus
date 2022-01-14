using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rebus.Timeouts;

/// <summary>
/// Represents the result of calling <see cref="ITimeoutManager.GetDueMessages"/> - contains zero or mode <see cref="DueMessage"/> objects
/// where each due message can be individually marked as successfully delivered 
/// </summary>
public class DueMessagesResult : IEnumerable<DueMessage>, IDisposable
{
    readonly List<DueMessage> _dueMessages;

    Func<Task> _cleanupAction;

    /// <summary>
    /// Constructs the result, wrapping the given list of due messages, performing the given action when the instance is disposed
    /// </summary>
    public DueMessagesResult(IEnumerable<DueMessage> dueMessages, Func<Task> cleanupAction = null)
    {
        _cleanupAction = cleanupAction;
        _dueMessages = dueMessages.ToList();
    }

    /// <summary>
    /// Gets an empty due messages result
    /// </summary>
    public static readonly DueMessagesResult Empty = new DueMessagesResult(Enumerable.Empty<DueMessage>());

    /// <summary>
    /// Completes the result by running the clean-up action
    /// </summary>
    public async Task Complete() => await CleanUp();

    /// <summary>
    /// Invokes the cleanup action
    /// </summary>
    public void Dispose() => CleanUp().Wait();

    /// <summary>
    /// Returns all due messages from this result
    /// </summary>
    public IEnumerator<DueMessage> GetEnumerator()
    {
        return _dueMessages.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    async Task CleanUp()
    {
        if (_cleanupAction == null) return;

        try
        {
            await _cleanupAction();
        }
        finally
        {
            _cleanupAction = null;
        }
    }
}