using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rebus.Pipeline
{
    public class DefaultPipeline : IPipeline
    {
        readonly List<StagedStep<IOutgoingStep, SendStage>> _sendSteps = new List<StagedStep<IOutgoingStep, SendStage>>();
        readonly List<StagedStep<IIncomingStep, ReceiveStage>> _receiveSteps = new List<StagedStep<IIncomingStep, ReceiveStage>>();

        public IEnumerable<StagedStep<IOutgoingStep, SendStage>> SendPipeline()
        {
            return _sendSteps.Select(s => new StagedStep<IOutgoingStep, SendStage>((IOutgoingStep)s.Step, SendStage.None));
        }

        public IEnumerable<StagedStep<IIncomingStep, ReceiveStage>> ReceivePipeline()
        {
            return _receiveSteps.Select(s => new StagedStep<IIncomingStep,ReceiveStage>((IIncomingStep)s.Step, (ReceiveStage)s.Stage));
        }

        public DefaultPipeline OnReceive(IIncomingStep step, ReceiveStage stage)
        {
            _receiveSteps.Add(new StagedStep<IIncomingStep, ReceiveStage>(step, stage));
            return this;
        }

        public DefaultPipeline OnReceive(Action<StepContext, Func<Task>> step, ReceiveStage stage, string stepDescription = null)
        {
            return OnReceive(new StepContainer(step, stepDescription), stage);
        }

        public DefaultPipeline OnSend(IOutgoingStep step)
        {
            _sendSteps.Add(new StagedStep<IOutgoingStep, SendStage>(step, SendStage.None));
            return this;
        }

        public DefaultPipeline OnSend(Action<StepContext, Func<Task>> step, string stepDescription = null)
        {
            return OnSend(new StepContainer(step, stepDescription));
        }

        public class StepContainer : IIncomingStep, IOutgoingStep
        {
            readonly Action<StepContext, Func<Task>> _step;
            readonly string _description;

            public StepContainer(Action<StepContext, Func<Task>> step, string description)
            {
                _step = step;
                _description = description;
            }

            public async Task Process(OutgoingStepContext context, Func<Task> next)
            {
                _step(context, next);
            }

            public async Task Process(IncomingStepContext context, Func<Task> next)
            {
                _step(context, next);
            }

            public override string ToString()
            {
                return string.Format("Step: {0}", _description);
            }
        }
    }
}