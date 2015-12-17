using GoCommando.Api;
using GoCommando.Attributes;
using Rebus.Logging;

namespace Rebus.Forklift.Common
{
    public abstract class ForkliftBase : ICommando
    {
        protected IRebusLoggerFactory LoggerFactory;

        [PositionalArgument]
        [Description("Name of queue to receive messages from")]
        [Example("some_queue")]
        [Example("remote_queue@another_machine")]
        public string InputQueue { get; set; }

        [NamedArgument("output", "o")]
        [Description("Default queue to forward messages to")]
        [Example("another_queue")]
        [Example("remote_queue@another_machine")]
        public string DefaultOutputQueue { get; set; }

        [NamedArgument("verbose", "v")]
        [Description("Enabled verbose logging")]
        public bool Verbose { get; set; }

        public void Run()
        {
            if (Verbose)
            {
                Text.PrintLine("Enabling verbose logging");

                LoggerFactory = new ConsoleLoggerFactory(true)
                {
                    ShowTimestamps = false,
                    MinLevel = LogLevel.Debug,
                };
            }
            else
            {
                Text.PrintLine("Verbose logging disabled (enable with -verbose)");

                LoggerFactory = new ConsoleLoggerFactory(true)
                {
                    MinLevel = LogLevel.Warn,
                    ShowTimestamps = false
                };
            }

            DoRun();
        }

        protected abstract void DoRun();
    }
}