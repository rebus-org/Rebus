using System;

namespace Rebus.Logging
{
    /// <summary>
    /// Represents a console color setting consisting of a foreground and a background color
    /// </summary>
    public class ColorSetting
    {
        ColorSetting(ConsoleColor foregroundColor)
        {
            ForegroundColor = foregroundColor;
        }

        /// <summary>
        /// Gets the foreground color
        /// </summary>
        public ConsoleColor ForegroundColor { get; }
        
        /// <summary>
        /// Gets the background color
        /// </summary>
        public ConsoleColor? BackgroundColor { get; private set; }

        /// <summary>
        /// Sets the foreground color to the specified color
        /// </summary>
        public static ColorSetting Foreground(ConsoleColor foregroundColor)
        {
            return new ColorSetting(foregroundColor);
        }

        /// <summary>
        /// Sets the background color to the specified color
        /// </summary>
        public ColorSetting Background(ConsoleColor backgroundColor)
        {
            BackgroundColor = backgroundColor;
            return this;
        }

        /// <summary>
        /// Sets the current console colors to those specified in this <see cref="ColorSetting"/>,
        /// restoring them to the previous colors when disposing
        /// </summary>
        public IDisposable Enter()
        {
            return new ConsoleColorContext(this);
        }

        class ConsoleColorContext : IDisposable
        {
            public ConsoleColorContext(ColorSetting colorSetting)
            {
                if (colorSetting.BackgroundColor.HasValue)
                {
                    Console.BackgroundColor = colorSetting.BackgroundColor.Value;
                }

                Console.ForegroundColor = colorSetting.ForegroundColor;
            }

            public void Dispose()
            {
                Console.ResetColor();
            }
        }
    }
}