using System;

namespace Rebus.Logging
{
    public class LoggingColors
    {
        public LoggingColors()
        {
            Debug = ColorSetting.Foreground(ConsoleColor.Gray);
            Info = ColorSetting.Foreground(ConsoleColor.Green);
            Warn = ColorSetting.Foreground(ConsoleColor.Yellow);
            Error = ColorSetting.Foreground(ConsoleColor.Red);
        }

        public ColorSetting Debug { get; set; }

        public ColorSetting Info { get; set; }
        
        public ColorSetting Warn { get; set; }
        
        public ColorSetting Error { get; set; }
    }
}