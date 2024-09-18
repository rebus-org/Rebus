using System;
using System.Linq;
using System.Text;

namespace Rebus.Extensions;

static class StringExtensions
{
    public static string Truncate(this string text, int maxLength, string placeholder = "(...)")
    {
        if (text.Length <= maxLength) return text;
        if (placeholder.Length >= maxLength) return placeholder.Substring(0, maxLength);

        var lengthOfTextToKeep = maxLength - placeholder.Length;

        if (lengthOfTextToKeep <= 0) return placeholder;

        return string.Concat(text.Substring(0, lengthOfTextToKeep), placeholder);
    }

    public static string WrappedAt(this string str, int width)
    {
        var twoLineBreaks = Environment.NewLine + Environment.NewLine;

        var sections = str.Split(new[] { twoLineBreaks },
            StringSplitOptions.RemoveEmptyEntries);

        return string.Join(twoLineBreaks, sections.Select(section => WrapSection(section, width)));
    }

    static string WrapSection(string section, int width)
    {
        var oneLongString = string.Join(" ",
            section.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));

        var words = oneLongString.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

        var builder = new StringBuilder();

        var currentLineLength = 0;

        for (var index = 0; index < words.Length; index++)
        {
            var word = words[index];
            builder.Append(word);
            currentLineLength += word.Length;

            if (index < words.Length - 1)
            {
                var nextWord = words[index];

                var spaceLeftOnCurrentLine = width - currentLineLength - 1; // -1 to leave room for space...
                var nextWordIsTooLong = nextWord.Length > spaceLeftOnCurrentLine;

                if (nextWordIsTooLong)
                {
                    builder.AppendLine();
                    currentLineLength = 0;
                }
                else
                {
                    builder.Append(' ');
                    currentLineLength++;
                }
            }
        }

        return builder.ToString();
    }

    public static string Indented(this string str, int indent)
    {
        var indentedLines = str
            .Split(new[] { Environment.NewLine }, StringSplitOptions.None)
            .Select(line => string.Concat(new string(' ', indent), line));

        return string.Join(Environment.NewLine, indentedLines);
    }
}