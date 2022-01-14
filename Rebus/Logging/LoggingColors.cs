using System;

namespace Rebus.Logging;

/// <summary>
/// Represents a set of colors to be used by the <see cref="ConsoleLoggerFactory"/> when running
/// in colored mode
/// </summary>
public class LoggingColors
{
    /// <summary>
    /// Constructs the default set of logging colors, which if gray, green, yellow, and red foreground for log
    /// levels debug, info, warn, and error respectively.
    /// </summary>
    public LoggingColors()
    {
        Debug = ColorSetting.Foreground(ConsoleColor.Gray);
        Info = ColorSetting.Foreground(ConsoleColor.Green);
        Warn = ColorSetting.Foreground(ConsoleColor.Yellow);
        Error = ColorSetting.Foreground(ConsoleColor.Red);
    }

    /// <summary>
    /// Gets/sets the color to use when printing DEBUG log statements
    /// </summary>
    public ColorSetting Debug { get; set; }

    /// <summary>
    /// Gets/sets the color to use when printing INFO log statements
    /// </summary>
    public ColorSetting Info { get; set; }

    /// <summary>
    /// Gets/sets the color to use when printing WARN log statements
    /// </summary>
    public ColorSetting Warn { get; set; }

    /// <summary>
    /// Gets/sets the color to use when printing ERROR log statements
    /// </summary>
    public ColorSetting Error { get; set; }
}