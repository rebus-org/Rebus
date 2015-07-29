using System;
using System.Collections.Generic;
using Rebus.Bus;
using Rebus.Messages;

namespace Rebus
{
    /// <summary>
    /// Delegate type that can listen to whenever the bus starts.
    /// </summary>
    public delegate void BusStartedEventHandler(IBus bus);
    
    /// <summary>
    /// Delegate type that can listen to whenever the bus stops.
    /// </summary>
    public delegate void BusStoppedEventHandler(IBus bus);
    
    /// <summary>
    /// Delegate type that can listen to whenever the bus sends a logical message.
    /// </summary>
    public delegate void MessageSentEventHandler(IBus bus, string destination, object message);

    /// <summary>
    /// Delegate type that can listen to whenever the bus sends a transport message.
    /// </summary>
    public delegate void BeforeInternalSendEventHandler(IEnumerable<string> destinations, Message message, bool published);
    
    /// <summary>
    /// Delegate type that can listen to whenever the bus received a logical message.
    /// </summary>
    public delegate void BeforeMessageEventHandler(IBus bus, object message);
    
    /// <summary>
    /// Delegate type that can listen to whenever the bus received a logical message.
    /// </summary>
    public delegate void AfterMessageEventHandler(IBus bus, Exception exception, object message);
    
    /// <summary>
    /// Delegate type that can listen to whenever the bus has received a transport message, but it has not yet been deserialized.
    /// </summary>
    public delegate void BeforeTransportMessageEventHandler(IBus bus, ReceivedTransportMessage receivedTransportMessage);
    
    /// <summary>
    /// Delegate type that can listen to whenever the bus has received and dispatched a transport message, and then - depending on how that
    /// went - and exception might be passed along.
    /// </summary>
    public delegate void AfterTransportMessageEventHandler(IBus bus, Exception exceptionOrNull, ReceivedTransportMessage receivedTransportMessage);
    
    /// <summary>
    /// Delegate type that can listen to whenever the bus has decided that message is poison, and should be moved to the error queue.
    /// </summary>
    public delegate void PoisonMessageEventHandler(IBus bus, ReceivedTransportMessage receivedTransportMessage, PoisonMessageInfo poisonMessageInfo);

    /// <summary>
    /// Delegate type that can listen when an incoming message can be handled by a saga handler, but it turns out that there was no saga data that could be correlated with the message.
    /// </summary>
    public delegate void UncorrelatedMessageEventHandler(IBus bus, object message, Saga saga);

    /// <summary>
    /// Delegate type that can listen to whenever a message context is established.
    /// </summary>
    public delegate void MessageContextEstablishedEventHandler(IBus bus, IMessageContext messageContext);

    /// <summary>
    /// Delegate type that can listen to whenever a message has been audited (i.e. is copied to the audit queue)
    /// </summary>
    public delegate void MessageAuditedEventHandler(IBus bus, TransportMessageToSend transportMessageToSend);

    /// <summary>
    /// Delegate type that can listen to whenever an exception is thrown during the execution of handler of a message.
    /// </summary>
    public delegate void OnHandlingErrorEventHandler(Exception exception);

    /// <summary>
    /// Delegate type that can listen to whenever a message handler has been executed.
    /// </summary>
    public delegate void AfterHandlingEventHandler(IBus bus, object message, IHandleMessages handler);

    /// <summary>
    /// Delegate type that can listen to whenever a message handler is going to be executed.
    /// </summary>
    public delegate void BeforeHandlingEventHandler(IBus bus, object message, IHandleMessages handler);

    /// <summary>
    /// Groups the different event hooks that Rebus exposes.
    /// </summary>
    public interface IRebusEvents
    {
        /// <summary>
        /// Event that will be raised immediately after bus is started
        /// and ready to handle messages
        /// </summary>
        event BusStartedEventHandler BusStarted;
        
        /// <summary>
        /// Event that will be raised when the bus is disposed
        /// </summary>
        event BusStoppedEventHandler BusStopped;
        
        /// <summary>
        /// Event that will be raised immediately after receiving a transport 
        /// message, before any other actions are executed.
        /// </summary>
        event BeforeTransportMessageEventHandler BeforeTransportMessage;

        /// <summary>
        /// Event that will be raised after a transport message has been handled.
        /// If an error occurs, the caught exception will be passed to the
        /// listeners. If no errors occur, the passed exception will be null.
        /// </summary>
        event AfterTransportMessageEventHandler AfterTransportMessage;
        
        /// <summary>
        /// Event that will be raised whenever the handled/published transport message has been copied to the audit queue
        /// </summary>
        event MessageAuditedEventHandler MessageAudited;

        /// <summary>
        /// Event that will be raised whenever it is determined that a message
        /// has failed too many times.
        /// </summary>
        event PoisonMessageEventHandler PoisonMessage;

        /// <summary>
        /// Event that will be raised immediately when the bus is used to send a logical message.
        /// </summary>
        event MessageSentEventHandler MessageSent;

        /// <summary>
        /// Event that will be raised immediately before a transport message is sent.
        /// </summary>
        event BeforeInternalSendEventHandler BeforeInternalSend;

        /// <summary>
        /// Event that will be raised for each received logical message (i.e. it will only be called
        /// if deserialization completed, and the transport message does in fact contain one or more
        /// logical messages).
        /// </summary>
        event BeforeMessageEventHandler BeforeMessage;

        /// <summary>
        /// Event that will be raised for each received logical message (i.e. it will only be called
        /// if deserialization completed, and the transport message does in fact contain one or more
        /// logical messages).
        /// </summary>
        event AfterMessageEventHandler AfterMessage;

        /// <summary>
        /// Event that is raised when an incoming message can be handled by a saga handler, but it
        /// turns out that no saga data instance could be correlated with the message.
        /// </summary>
        event UncorrelatedMessageEventHandler UncorrelatedMessage;
        
        /// <summary>
        /// Event that is raised when an incoming transport message has been properly deserialized,
        /// and it is about to be dispatched. The message context will last for the duration of the
        /// message processing and is disposed at the very end.
        /// </summary>
        event MessageContextEstablishedEventHandler MessageContextEstablished;

        /// <summary>
        /// Event that is raised when an exception is thrown during the handling of a message.
        /// </summary>
        event OnHandlingErrorEventHandler OnHandlingError;

        /// <summary>
        /// Event that is raised after the execution of a handler of a message.
        /// </summary>
        event AfterHandlingEventHandler AfterHandling;

        /// <summary>
        /// Event that is raised before the execution of a handler of a message.
        /// </summary>
        event BeforeHandlingEventHandler BeforeHandling;

        /// <summary>
        /// Contains a pipeline of message mutators that will be run in order when messages are sent,
        /// and in reverse order when messages are received.
        /// </summary>
        ICollection<IMutateMessages> MessageMutators { get; }

        /// <summary>
        /// Adds a unit of work manager that will be allowed to create a unit of work for each incoming message
        /// </summary>
        void AddUnitOfWorkManager(IUnitOfWorkManager unitOfWorkManager);
    }
}