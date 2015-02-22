using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rebus2.Pipeline
{
    public class DefaultPipeline : IPipeline
    {
        readonly List<RegisteredStep> _sendSteps = new List<RegisteredStep>();
        readonly List<RegisteredStep> _receiveSteps = new List<RegisteredStep>();

        public IEnumerable<StagedStep<IOutgoingStep>> SendPipeline()
        {
            return _sendSteps.Select(s => new StagedStep<IOutgoingStep>((IOutgoingStep)s.Step, (ReceiveStage)s.Stage));
        }

        public IEnumerable<StagedStep<IIncomingStep>> ReceivePipeline()
        {
            return _receiveSteps.Select(s => new StagedStep<IIncomingStep>((IIncomingStep)s.Step, (ReceiveStage)s.Stage));
        }

        public DefaultPipeline OnReceive(IStep step, ReceiveStage stage)
        {
            _receiveSteps.Add(new RegisteredStep(step, (int)stage));
            return this;
        }

        public DefaultPipeline OnReceive(Action<StepContext, Func<Task>> step, ReceiveStage stage, string stepDescription = null)
        {
            return OnReceive(new StepContainer(step, stepDescription), stage);
        }

        public DefaultPipeline OnSend(IStep step)
        {
            _sendSteps.Add(new RegisteredStep(step, 0));
            return this;
        }

        public DefaultPipeline OnSend(Action<StepContext, Func<Task>> step, string stepDescription = null)
        {
            return OnSend(new StepContainer(step, stepDescription));
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
            public RegisteredStep(IStep step, int stage)
            {
                Step = step;
                Stage = stage;
            }

            public IStep Step { get; private set; }
            public int Stage { get; private set; }
        }
    }
}