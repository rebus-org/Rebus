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
    /// Gets whether the message's <see cref="Headers.ReturnAddress"/> header is set to something
    /// </summary>
    public static bool HasReturnAddress( this Message message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        return message.Headers.ContainsKey(Headers.ReturnAddress);
    }

    /// <summary>
    /// Uses the transport's input queue address as the <see cref="Headers.ReturnAddress"/> on the message
    /// </summary>
    public static void SetReturnAddressFromTransport( this Message message,  ITransport transport)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (transport == null) throw new ArgumentNullException(nameof(transport));

        var returnAddress = transport.Address;

        if (string.IsNullOrWhiteSpace(returnAddress))
        {
            throw new InvalidOperationException("Cannot set return address from the given transport because it is not capable of receiving messages");
        }

        message.Headers[Headers.ReturnAddress] = returnAddress;
    }

    /// <summary>
    /// Sets the <see cref="Headers.DeferredUntil"/> header to the specified time
    /// </summary>
    public static void SetDeferHeaders(this Message message, DateTimeOffset approximateDeliveryTime, string destinationAddress)
    {
        InnerSetDeferHeaders(approximateDeliveryTime, message.Headers, destinationAddress);
    }

    /// <summary>
    /// Sets the <see cref="Headers.DeferredUntil"/> header to the specified time
    /// </summary>
    public static void SetDeferHeaders(this TransportMessage message, DateTimeOffset approximateDeliveryTime, string destinationAddress)
    {
        InnerSetDeferHeaders(approximateDeliveryTime, message.Headers, destinationAddress);
    }

    static void InnerSetDeferHeaders(DateTimeOffset approximateDeliveryTime, Dictionary<string, string> headers, string destinationAddress)
    {
        headers[Headers.DeferredUntil] = approximateDeliveryTime.ToIso8601DateTimeOffset();

        // do not overwrite the recipient if it has been set
        if (!headers.ContainsKey(Headers.DeferredRecipient))
        {
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
        return message.Headers.GetValueOrNull(Headers.Type)
               ?? GetTypeNameFromBodyObjectOrNull(message.Body)
               ?? "<unknown>";
    }

    /// <summary>
    /// Gets the message type from the message
    /// </summary>
    public static string GetMessageType(this TransportMessage message)
    {
        return message.Headers.GetValueOrNull(Headers.Type)
               ?? "<unknown>";
    }

    /// <summary>
    /// Gets the message ID from the message
    /// </summary>
    public static string GetMessageId(this Message message)
    {
        return message.Headers.GetValue(Headers.MessageId);
    }

    /// <summary>
    /// Gets the message ID from the message
    /// </summary>
    public static string GetMessageId(this TransportMessage message)
    {
        return message.Headers.GetValue(Headers.MessageId);
    }

    /// <summary>
    /// Gets a nice label for the message, consisting of message type and ID if possible
    /// </summary>
    public static string GetMessageLabel(this Message message)
    {
        return GetMessageLabel(message.Headers);
    }

    /// <summary>
    /// Gets a nice label for the message, consisting of message type and ID if possible
    /// </summary>
    public static string GetMessageLabel(this TransportMessage message)
    {
        return GetMessageLabel(message.Headers);
    }

    /// <summary>
    /// Returns a cloned instance of the transport message
    /// </summary>
    public static TransportMessage Clone(this TransportMessage message)
    {
        return new TransportMessage(message.Headers.Clone(), message.Body);
    }

    static string GetMessageLabel(Dictionary<string, string> headers)
    {
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

    static string GetTypeNameFromBodyObjectOrNull(object body)
    {
        return body?.GetType().GetSimpleAssemblyQualifiedName();
    }
}