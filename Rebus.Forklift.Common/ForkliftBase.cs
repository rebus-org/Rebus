using GoCommando.Attributes;
using Rebus.Logging;

namespace Rebus.Forklift.Common
{
    public abstract class ForkliftBase
    {
        protected ForkliftBase()
        {
            RebusLoggerFactory.Current = new NullLoggerFactory();
        }

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
 
    }
}