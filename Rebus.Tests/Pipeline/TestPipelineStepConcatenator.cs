using System;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Pipeline;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Pipeline
{
    public class TestPipelineStepConcatenator
    {
        [Fact]
        public void CanInjectStepInTheFront()
        {
            var pipeline = new DefaultPipeline()
                .OnReceive(new Step1())
                .OnReceive(new Step2());

            var injector = new PipelineStepConcatenator(pipeline)
                .OnReceive(new InjectedStep(), PipelineAbsolutePosition.Front);

            var receivePipeline = injector.ReceivePipeline().ToArray();

            Assert.Equal(new[]
                {
                    typeof(InjectedStep),
                    typeof(Step1),
                    typeof(Step2),
                },
                receivePipeline.Select(s => s.GetType()));
        }

        [Fact]
        public void CanInjectStepInTheBack()
        {
            var pipeline = new DefaultPipeline()
                .OnReceive(new Step1())
                .OnReceive(new Step2());

            var injector = new PipelineStepConcatenator(pipeline)
                .OnReceive(new InjectedStep(), PipelineAbsolutePosition.Back);

            var receivePipeline = injector.ReceivePipeline().ToArray();

            Assert.Equal(new[]
                {
                    typeof(Step1),
                    typeof(Step2),
                    typeof(InjectedStep),
                },
                receivePipeline.Select(s => s.GetType()));
        }

        class Step1 : IIncomingStep
        {
            public async Task Process(IncomingStepContext context, Func<Task> next) { }
        }

        class Step2 : IIncomingStep
        {
            public async Task Process(IncomingStepContext context, Func<Task> next) { }
        }

        class InjectedStep : IIncomingStep
        {
            public async Task Process(IncomingStepContext context, Func<Task> next) { }
        }
    }
}