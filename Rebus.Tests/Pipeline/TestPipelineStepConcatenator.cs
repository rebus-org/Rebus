using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Pipeline;
#pragma warning disable 1998

namespace Rebus.Tests.Pipeline;

[TestFixture]
public class TestPipelineStepConcatenator
{
    [Test]
    public void CanInjectStepInTheFront()
    {
        var pipeline = new DefaultPipeline()
            .OnReceive(new Step1())
            .OnReceive(new Step2());

        var injector = new PipelineStepConcatenator(pipeline)
            .OnReceive(new InjectedStep(), PipelineAbsolutePosition.Front);

        var receivePipeline = injector.ReceivePipeline().ToArray();

        Assert.That(receivePipeline.Select(s => s.GetType()), Is.EqualTo(new[]
        {
            typeof(InjectedStep),
            typeof(Step1),
            typeof(Step2),
        }));
    }

    [Test]
    public void CanInjectStepInTheBack()
    {
        var pipeline = new DefaultPipeline()
            .OnReceive(new Step1())
            .OnReceive(new Step2());

        var injector = new PipelineStepConcatenator(pipeline)
            .OnReceive(new InjectedStep(), PipelineAbsolutePosition.Back);

        var receivePipeline = injector.ReceivePipeline().ToArray();

        Assert.That(receivePipeline.Select(s => s.GetType()), Is.EqualTo(new[]
        {
            typeof(Step1),
            typeof(Step2),
            typeof(InjectedStep),
        }));
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