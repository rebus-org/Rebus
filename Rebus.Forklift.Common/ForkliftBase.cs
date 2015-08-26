using GoCommando.Api;
using GoCommando.Attributes;
using Rebus.Logging;

namespace Rebus.Forklift.Common
{
    public abstract class ForkliftBase : ICommando
    {
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
                
                RebusLoggerFactory.Current = new ConsoleLoggerFactory(true)
                {
                    MinLevel = LogLevel.Debug,
                    ShowTimestamps = false
                };
            }
            else
            {
                Text.PrintLine("Verbose logging disabled (enable with -verbose)");
                
                RebusLoggerFactory.Current = new ConsoleLoggerFactory(true)
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