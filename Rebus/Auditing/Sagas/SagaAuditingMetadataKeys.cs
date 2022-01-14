namespace Rebus.Auditing.Sagas;

/// <summary>
/// Contains keys of metadata elements that can be found alongside saga data snapshots when saga auditing has been enabled
/// </summary>
public class SagaAuditingMetadataKeys
{
    /// <summary>
    /// Key of the input queue address of the endpoint that handled the message that brought the saga data to the current state
    /// </summary>
    public const string HandleQueue = "HandleQueue";
        
    /// <summary>
    /// Key of the type of the saga data
    /// </summary>
    public const string SagaDataType = "SagaDataType";
        
    /// <summary>
    /// Key of the type of the saga handler that brought the saga data to the current state
    /// </summary>
    public const string SagaHandlerType = "SagaHandlerType";

    /// <summary>
    /// Key of the type of the message that brought the saga data to the current state
    /// </summary>
    public const string MessageType = "MessageType";

    /// <summary>
    /// Key of the ID of the message that brought the saga data to the current state
    /// </summary>
    public const string MessageId = "MessageId";

    /// <summary>
    /// Key of the machine name on which the saga data was brought to the current state
    /// </summary>
    public const string MachineName = "MachineName";
}