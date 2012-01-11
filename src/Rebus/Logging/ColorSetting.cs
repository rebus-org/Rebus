using System;

namespace Rebus.Logging
{
    public class ColorSetting
    {
        ColorSetting(ConsoleColor foregroundColor)
        {
            ForegroundColor = foregroundColor;
        }

        public ConsoleColor ForegroundColor { get; set; }
        
        public ConsoleColor? BackgroundColor { get; set; }

        public static ColorSetting Foreground(ConsoleColor foregroundColor)
        {
            return new ColorSetting(foregroundColor);
        }

        public ColorSetting Background(ConsoleColor backgroundColor)
        {
            BackgroundColor = backgroundColor;
            return this;
        }

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