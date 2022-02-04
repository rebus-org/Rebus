using System;
using Rebus.Logging;

namespace Rebus.Tests.Contracts.Utilities;

public class LogLine
{
    public DateTime Time { get; }
    public LogLevel Level { get; }
    public Type Type { get; }
    public string Text { get; }

    public LogLine(LogLevel level, string text, Type type)
    {
        Time = DateTime.Now;
        Level = level;
        Text = text;
        Type = type;
    }

    public override string ToString() => $"{Level} / {Type} / {string.Join(" | ", Text.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))}";
}