using System;
using System.Collections.Generic;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.Bus;

/// <summary>
/// Small helpers that make it easier to work with the <see cref="Message"/> class
/// </summary>
public static class MessageExtensions
{
    /// <summary>
    /// Sets the <see cref="Headers.DeferredUntil"/> header to the specified time
    /// </summary>
    public static void SetDeferHeaders(this Message message, DateTimeOffset approximateDeliveryTime, string destinationAddress)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        InnerSetDeferHeaders(approximateDeliveryTime, message.Headers, destinationAddress);
    }

    /// <summary>
    /// Sets the <see cref="Headers.DeferredUntil"/> header to the specified time
    /// </summary>
    public static void SetDeferHeaders(this TransportMessage message, DateTimeOffset approximateDeliveryTime, string destinationAddress)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        InnerSetDeferHeaders(approximateDeliveryTime, message.Headers, destinationAddress);
    }

    static void InnerSetDeferHeaders(DateTimeOffset approximateDeliveryTime, Dictionary<string, string> headers, string destinationAddress)
    {
        if (headers == null) throw new ArgumentNullException(nameof(headers));

        headers[Headers.DeferredUntil] = approximateDeliveryTime.ToIso8601DateTimeOffset();

        // do not overwrite the recipient if it has been set
        if (!headers.ContainsKey(Headers.DeferredRecipient))
        {
            // if a recipient is not specified explicitly, it must be passed to this method... fail with an IOE if that is not the case
            if (string.IsNullOrWhiteSpace(destinationAddress))
            {
                throw new InvalidOperationException($"The {nameof(destinationAddress)} parameter did not have a value, in which case it is required that the '{Headers.DeferredRecipient}' header is" +
                                                    " explicitly set to the queue name of the recipient of the message. This error can typically happen when a one-way client defers a message," +
                                                    $" and the message is not explicitly routed somewhere. Please either pass a recipient queue name as the '{Headers.DeferredRecipient}' headers," +
                                                    " or bind this particular message type to a destination queue.");
            }

            headers[Headers.DeferredRecipient] = destinationAddress;
        }

        // if the headers indicate that this message has been deferred before, we increment the count
        if (int.TryParse(headers.GetValueOrNull(Headers.DeferCount), out var deferCount))
        {
            headers[Headers.DeferCount] = (deferCount + 1).ToString();
        }
        else
        {
            // otherwise we set to 1
            headers[Headers.DeferCount] = "1";
        }
    }

    /// <summary>
    /// Gets the message type from the message
    /// </summary>
    public static string GetMessageType(this Message message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        return message.Headers.GetValueOrNull(Headers.Type)
               ?? GetTypeNameFromBodyObjectOrNull(message.Body)
               ?? "<unknown>";
    }

    /// <summary>
    /// Gets the message type from the message
    /// </summary>
    public static string GetMessageType(this TransportMessage message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        return message.Headers.GetValueOrNull(Headers.Type)
               ?? "<unknown>";
    }

    /// <summary>
    /// Gets the message ID from the message
    /// </summary>
    public static string GetMessageId(this Message message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        return message.Headers.GetValue(Headers.MessageId);
    }

    /// <summary>
    /// Gets the message ID from the message
    /// </summary>
    public static string GetMessageId(this TransportMessage message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        return message.Headers.GetValue(Headers.MessageId);
    }

    /// <summary>
    /// Gets a nice label for the message, consisting of message type and ID if possible
    /// </summary>
    public static string GetMessageLabel(this Message message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        return GetMessageLabel(message.Headers);
    }

    /// <summary>
    /// Gets a nice label for the message, consisting of message type and ID if possible
    /// </summary>
    public static string GetMessageLabel(this TransportMessage message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        return GetMessageLabel(message.Headers);
    }

    /// <summary>
    /// Returns a cloned instance of the transport message
    /// </summary>
    public static TransportMessage Clone(this TransportMessage message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        return new TransportMessage(message.Headers.Clone(), message.Body);
    }

    static string GetMessageLabel(Dictionary<string, string> headers)
    {
        if (headers == null) throw new ArgumentNullException(nameof(headers));
        var id = headers.GetValueOrNull(Headers.MessageId) ?? "<unknown>";

        if (headers.TryGetValue(Headers.Type, out var type))
        {
            var dotnetType = Type.GetType(type);

            if (dotnetType != null)
            {
                type = dotnetType.Name;
            }
        }
        else
        {
            type = "<unknown>";
        }

        return $"{type}/{id}";
    }

    static string GetTypeNameFromBodyObjectOrNull(object body) => body?.GetType().GetSimpleAssemblyQualifiedName();
}