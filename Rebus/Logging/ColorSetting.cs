using System;

namespace Rebus.Logging;

/// <summary>
/// Represents a console color setting consisting of a foreground and a background color
/// </summary>
public class ColorSetting
{
    ColorSetting(ConsoleColor foregroundColor, ConsoleColor? backgroundColor = null)
    {
        ForegroundColor = foregroundColor;
        BackgroundColor = backgroundColor;
    }

    /// <summary>
    /// Gets the foreground color
    /// </summary>
    public ConsoleColor ForegroundColor { get; }

    /// <summary>
    /// Gets the background color
    /// </summary>
    public ConsoleColor? BackgroundColor { get; }

    /// <summary>
    /// Sets the foreground color to the specified color
    /// </summary>
    public static ColorSetting Foreground(ConsoleColor foregroundColor) => new ColorSetting(foregroundColor);

    /// <summary>
    /// Sets the background color to the specified color
    /// </summary>
    public ColorSetting Background(ConsoleColor backgroundColor) => new ColorSetting(ForegroundColor, backgroundColor);

    /// <summary>
    /// Sets the foreground (and possibly background too) colors of the console
    /// </summary>
    public void Apply()
    {
        if (BackgroundColor.HasValue)
        {
            Console.BackgroundColor = BackgroundColor.Value;
        }

        Console.ForegroundColor = ForegroundColor;
    }

    /// <summary>
    /// Resets the console colors to their normal values
    /// </summary>
    public void Revert() => Console.ResetColor();
}