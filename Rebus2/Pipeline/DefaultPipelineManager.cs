using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rebus2.Pipeline
{
    public class DefaultPipelineManager : IPipelineManager
    {
        readonly List<RegisteredStep> _sendSteps = new List<RegisteredStep>();
        readonly List<RegisteredStep> _receiveSteps = new List<RegisteredStep>();

        public IEnumerable<IStep> SendPipeline()
        {
            return _sendSteps.Select(s => s.Step);
        }

        public IEnumerable<IStep> ReceivePipeline()
        {
            return _receiveSteps.Select(s => s.Step);
        }

        public DefaultPipelineManager Receive(IStep step)
        {
            _receiveSteps.Add(new RegisteredStep { Step = step });
            return this;
        }

        public DefaultPipelineManager Receive(Action<StepContext, Func<Task>> step, string stepDescription = null)
        {
            return Receive(new StepContainer(step, stepDescription));
        }

        public class StepContainer : IStep
        {
            readonly Action<StepContext, Func<Task>> _step;
            readonly string _description;

            public StepContainer(Action<StepContext, Func<Task>> step, string description)
            {
                _step = step;
                _description = description;
            }

            public async Task Process(StepContext context, Func<Task> next)
            {
                _step(context, next);
            }

            public override string ToString()
            {
                return string.Format("Step: {0}", _description);
            }
        }

        class RegisteredStep
        {
            public IStep Step { get; set; }
        }
    }
}