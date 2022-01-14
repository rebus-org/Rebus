using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Pipeline;
#pragma warning disable 1998

namespace Rebus.Tests.Pipeline;

[TestFixture]
public class TestPipelineStepInjector
{
    [Test]
    public void CanInjectStepBeforeAnotherStep()
    {
        var pipeline = new DefaultPipeline()
            .OnReceive(new Step1())
            .OnReceive(new Step2())
            .OnReceive(new Step3());

        var injector = new PipelineStepInjector(pipeline)
            .OnReceive(new InjectedStep(), PipelineRelativePosition.Before, typeof(Step2));

        var receivePipeline = injector.ReceivePipeline().ToArray();

        Assert.That(receivePipeline.Select(s => s.GetType()), Is.EqualTo(new[]
        {
            typeof(Step1),
            typeof(InjectedStep),
            typeof(Step2),
            typeof(Step3),
        }));
    }

    [Test]
    public void CanInjectStepAfterAnotherStep()
    {
        var pipeline = new DefaultPipeline()
            .OnReceive(new Step1())
            .OnReceive(new Step2())
            .OnReceive(new Step3());

        var injector = new PipelineStepInjector(pipeline)
            .OnReceive(new InjectedStep(), PipelineRelativePosition.After, typeof(Step2));

        var receivePipeline = injector.ReceivePipeline().ToArray();

        Assert.That(receivePipeline.Select(s => s.GetType()), Is.EqualTo(new[]
        {
            typeof(Step1),
            typeof(Step2),
            typeof(InjectedStep),
            typeof(Step3),
        }));
    }

    [Test]
    public void CanInjectMultipleSteps()
    {
        var pipeline = new DefaultPipeline()
            .OnReceive(new Step1())
            .OnReceive(new Step2())
            .OnReceive(new Step3());

        var injector = new PipelineStepInjector(pipeline)
            .OnReceive(new InjectedStep(), PipelineRelativePosition.Before, typeof(Step2))
            .OnReceive(new InjectedStep(), PipelineRelativePosition.After, typeof(Step2));

        var receivePipeline = injector.ReceivePipeline().ToArray();

        Assert.That(receivePipeline.Select(s => s.GetType()), Is.EqualTo(new[]
        {
            typeof(Step1),
            typeof(InjectedStep),
            typeof(Step2),
            typeof(InjectedStep),
            typeof(Step3),
        }));
    }

    [Test]
    public void ThrowsExceptionIfAnchorCannotBeFound()
    {
        var pipeline = new DefaultPipeline()
            .OnReceive(new Step1())
            .OnReceive(new Step2());

        var injector = new PipelineStepInjector(pipeline)
            .OnReceive(new InjectedStep(), PipelineRelativePosition.After, typeof(Step3));

        var argumentException = Assert.Throws<ArgumentException>(() => injector.ReceivePipeline());

        Console.WriteLine(argumentException);
    }

    class Step1 : IIncomingStep
    {
        public async Task Process(IncomingStepContext context, Func<Task> next) { }
    }

    class Step2 : IIncomingStep, IMarker
    {
        public async Task Process(IncomingStepContext context, Func<Task> next) { }
    }

    class Step3 : IIncomingStep
    {
        public async Task Process(IncomingStepContext context, Func<Task> next) { }
    }

    class InjectedStep : IIncomingStep
    {
        public async Task Process(IncomingStepContext context, Func<Task> next) { }
    }

    interface IMarker
    {
    }
}