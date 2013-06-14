namespace Rebus.Configuration
{
    /// <summary>
    /// Contains additional behavioral stuff that affects how Rebus does its work
    /// </summary>
    public class ConfigureAdditionalBehavior
    {
        /// <summary>
        /// Creates an instance of this behavior thingie with all the defaults set
        /// </summary>
        public ConfigureAdditionalBehavior()
        {
            HandleMessagesInTransactionScope = false;
        }

        /// <summary>
        /// Configures whether Rebus should create a transaction scope around the handling of transport messages.
        /// Defaults to false.
        /// </summary>
        public bool HandleMessagesInTransactionScope { get; set; }
    }
}