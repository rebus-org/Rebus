using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Rebus.Logging
{
    /// <summary>
    /// If you intend to implement your own logging, you probably want to derive from this class and implement <seealso cref="GetLogger"/>
    /// </summary>
    public abstract class AbstractRebusLoggerFactory : IRebusLoggerFactory
    {
        static readonly Regex PlaceholderRegex = new Regex(@"{\w*}", RegexOptions.Compiled);

        /// <inheritdoc />
        protected abstract ILog GetLogger(Type type);

        /// <inheritdoc />
        public ILog GetLogger<T>()
        {
            return GetLogger(typeof(T));
        }

        /// <summary>
        /// Renders the <paramref name="message"/> string by replacing placeholders on the form <code>{whatever}</code> with the
        /// string representation of each object from <paramref name="objs"/>. Note that the actual content of the placeholders
        /// is ignored - i.e. it doesn't matter whether it says <code>{0}</code>, <code>{name}</code>, or <code>{whatvgejigoejigoejigoe}</code>
        /// - values are interpolated based on their order regardless of the name of the placeholder.
        /// </summary>
        protected string RenderString(string message, object[] objs)
        {
            try
            {
                var index = 0;
                return PlaceholderRegex.Replace(message, match =>
                {
                    try
                    {
                        var value = objs[index];
                        index++;
                        return FormatObject(value);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        return "???";
                    }
                });
            }
            catch
            {
                return message;
            }
        }

        /// <summary>
        /// Formatter function that is invoked for each object value to be rendered into a string while interpolating log lines
        /// </summary>
        protected virtual string FormatObject(object obj)
        {
            if (obj is DateTime)
            {
                return ((DateTime) obj).ToString("O");
            }
            if (obj is DateTimeOffset)
            {
                return ((DateTimeOffset) obj).ToString("O");
            }
            if (obj is IConvertible)
            {
                return ((IConvertible)obj).ToString(CultureInfo.InvariantCulture);
            }
            if (obj is IFormattable)
            {
                return ((IFormattable)obj).ToString(null, CultureInfo.InvariantCulture);
            }
            return obj.ToString();
        }
    }
}