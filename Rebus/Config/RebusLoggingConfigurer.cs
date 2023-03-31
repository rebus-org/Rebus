using System;
using Rebus.Injection;
using Rebus.Logging;

namespace Rebus.Config;

/// <summary>
/// Configurer that is used to configure logging. This configurer is cheating a little bit because it will actually modify a global logger which will
/// be used throughout all Rebus instances. This mechanism might change in the future
/// </summary>
public class RebusLoggingConfigurer
{
    readonly Injectionist _injectionist;

    internal RebusLoggingConfigurer(Injectionist injectionist)
    {
        _injectionist = injectionist ?? throw new ArgumentNullException(nameof(injectionist));
    }

    /// <summary>
    /// Configures Rebus to log its stuff to stdout, possibly ignore logged lines under the specified <see cref="LogLevel"/>
    /// </summary>
    public void Console(LogLevel minLevel = LogLevel.Debug)
    {
        Use(new ConsoleLoggerFactory(false)
        {
            MinLevel = minLevel
        });
    }

    /// <summary>
    /// Configures Rebus to log its stuff to with different colors depending on the log level, possibly ignore logged lines under the specified <see cref="LogLevel"/>
    /// </summary>
    public void ColoredConsole(LogLevel minLevel = LogLevel.Debug)
    {
        Use(new ConsoleLoggerFactory(true)
        {
            MinLevel = minLevel
        });
    }

    /// <summary>
    /// Configures Rebus to log its stuff to <see cref="System.Diagnostics.Trace"/>
    /// </summary>
    public void Trace()
    {
        Use(new TraceLoggerFactory());
    }

    /// <summary>
    /// Disables logging alltogether
    /// </summary>
    public void None()
    {
        Use(new NullLoggerFactory());
    }

    /// <summary>
    /// Configures this Rebus instance to use the specified logger factory
    /// </summary>
    public void Use(IRebusLoggerFactory rebusLoggerFactory)
    {
        if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));
        _injectionist.Register(c => rebusLoggerFactory, $"This Rebus instance has been configured to use the {rebusLoggerFactory} logger factory");
    }

    /// <summary>
    /// Registers the given factory function as a resolve of the given <typeparamref name="TService"/> service
    /// </summary>
    public void Register<TService>(Func<IResolutionContext, TService> factoryMethod, string description = null)
    {
        _injectionist.Register(factoryMethod, description: description);
    }

    /// <summary>
    /// Registers the given factory function as a resolve of the given <typeparamref name="TService"/> service
    /// </summary>
    public void Decorate<TService>(Func<IResolutionContext, TService> factoryMethod, string description = null)
    {
        _injectionist.Decorate(factoryMethod, description: description);
    }
}