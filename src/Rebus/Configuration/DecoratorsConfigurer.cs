namespace Rebus.Configuration
{
    /// <summary>
    /// Configurer that allows for decorators to be added in the form of "decoration steps"
    /// </summary>
    public class DecoratorsConfigurer : BaseConfigurer
    {
        /// <summary>
        /// Constructs the decorators configurer
        /// </summary>
        public DecoratorsConfigurer(ConfigurationBackbone backbone)
            : base(backbone)
        {
        }
    }
}