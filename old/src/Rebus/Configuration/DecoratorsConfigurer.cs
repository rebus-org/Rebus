namespace Rebus.Configuration
{
    /// <summary>
    /// Configurer that allows for decorators to be added in the form of "decoration steps"
    /// </summary>
    public class DecoratorsConfigurer : BaseConfigurer
    {
        internal DecoratorsConfigurer(ConfigurationBackbone backbone)
            : base(backbone)
        {
        }
    }
}