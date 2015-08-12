namespace Rebus.Configuration
{
    /// <summary>
    /// Can be used to store code snippets meant to be shown in error messages. Embedded code snippets should be replaced
    /// by usage of consts from this class.
    /// </summary>
    public class HelpText
    {
        /// <summary>
        /// Shows a very basic and explicit example on how Rebus can be configured to use MSMQ.
        /// </summary>
        public const string TransportConfigurationExample = @"
    var bus = Configure.With(someContainerAdapter)
                .Transport(s => s.UseMsmq(""some_input_queue_name"", ""some_error_queue_name""))
                (....)
                .CreateBus()
                .Start();
";
    }
}