namespace Rebus.HttpGateway
{
    public class ErrorText
    {
        public const string GenericStartStopErrorHelpText = 
            "It's not that I want to cause you trouble, but this is usually an indication that some of your application"
            + " startup/shutdown is not completely clean and predictable, which is probably not what you really want."
            + " Please ensure that the service is started only once when the application starts, and stopped only once"
            + " when the application shuts down.";
    }
}