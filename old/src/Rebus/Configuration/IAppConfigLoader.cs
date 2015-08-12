namespace Rebus.Configuration
{
    /// <summary>
    /// Abstracts away how an application's app.config is loaded in a dynamic way
    /// </summary>
    public interface IAppConfigLoader
    {
        /// <summary>
        /// Loads the AppDomain's current app.config and returns the contents as a string
        /// </summary>
        string LoadIt();
    }
}