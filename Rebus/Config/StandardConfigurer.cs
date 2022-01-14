using System;
using Rebus.Injection;
using Rebus.Subscriptions;
using Rebus.Transport;

namespace Rebus.Config;

/// <summary>
/// Configurer that can have extension methods attached to it for helping with registering an implementation or a decorator
/// for the <typeparamref name="TService"/> service.
/// </summary>
public class StandardConfigurer<TService>
{
    /// <summary>
    /// Gets a standard configurer from the given options configurer. Can be used to provide
    /// extensions to <see cref="OptionsConfigurer"/> that return a standard configurer that can then
    /// be used to build further
    /// </summary>
    public static StandardConfigurer<TService> GetConfigurerFrom(OptionsConfigurer configurer)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));
        return configurer.GetConfigurer<TService>();
    }

    readonly Injectionist _injectionist;
    readonly Options _options;

    internal StandardConfigurer(Injectionist injectionist, Options options)
    {
        if (injectionist == null) throw new ArgumentNullException(nameof(injectionist));
        if (options == null) throw new ArgumentNullException(nameof(options));
        _injectionist = injectionist;
        _options = options;
    }

    internal Options Options => _options;

    /// <summary>
    /// Registers the given factory function as a resolve of the given <typeparamref name="TService"/> service
    /// </summary>
    public void Register(Func<IResolutionContext, TService> factoryMethod, string description = null)
    {
        if (factoryMethod == null) throw new ArgumentNullException(nameof(factoryMethod));
        _injectionist.Register(factoryMethod, description: description);
    }

    /// <summary>
    /// Registers the given factory function as a resolve of the given <typeparamref name="TService"/> service
    /// </summary>
    public void Decorate(Func<IResolutionContext, TService> factoryMethod, string description = null)
    {
        if (factoryMethod == null) throw new ArgumentNullException(nameof(factoryMethod));
        _injectionist.Decorate(factoryMethod, description: description);
    }

    /// <summary>
    /// Gets a typed configurer for another service. Can be useful if e.g. a configuration extension for a <see cref="ITransport"/>
    /// wants to replace the <see cref="ISubscriptionStorage"/> because it is capable of using the transport layer to do pub/sub
    /// </summary>
    public StandardConfigurer<TOther> OtherService<TOther>()
    {
        return new StandardConfigurer<TOther>(_injectionist, _options);
    }
}