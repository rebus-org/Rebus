namespace Rebus.RabbitMq
{
    /// <summary>
    /// Allows for fluently configuring RabbitMQ options
    /// </summary>
    public class RabbitMqOptionsBuilder
    {
        ///// <summary>
        ///// Sets max for how many messages the RabbitMQ driver should download in the background
        ///// </summary>
        //public RabbitMqOptionsBuilder SetPrefetch(int numberOfMessagesToprefetch)
        //{
        //    NumberOfMessagesToprefetch = numberOfMessagesToprefetch;
        //    return this;
        //}

        internal int? NumberOfMessagesToprefetch { get; set; }
    }
}