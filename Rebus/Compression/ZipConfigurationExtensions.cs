using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Rebus.Config;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;

namespace Rebus.Compression
{
    /// <summary>
    /// Configuration extensions for enabling compression
    /// </summary>
    public static class ZipConfigurationExtensions
    {
        /// <summary>
        /// Default threshold for the body size for compression to kick in
        /// </summary>
        public const int DefaultBodyThresholdBytes = 1024;

        /// <summary>
        /// Enables compression of outgoing messages if the size exceeds the specified number of bytes
        /// (defaults to <see cref="DefaultBodyThresholdBytes"/>)
        /// </summary>
        public static OptionsConfigurer EnableCompression(this OptionsConfigurer configurer,
            int bodySizeThresholdBytes = DefaultBodyThresholdBytes)
        {
            configurer.Register(c => new Zipper());
            configurer.Register(c => new UnzipMessagesIncomingStep(c.Get<Zipper>()));
            configurer.Register(c => new ZipMessagesOutgoingStep(c.Get<Zipper>(), bodySizeThresholdBytes));

            configurer.Decorate<IPipeline>(c => new PipelineStepInjector(c.Get<IPipeline>())
                .OnReceive(c.Get<UnzipMessagesIncomingStep>(), Position.Before, typeof(DeserializeIncomingMessageStep))
                .OnSend(c.Get<ZipMessagesOutgoingStep>(), Position.After, typeof(SerializeOutgoingMessageStep)));

            return configurer;
        }

        /// <summary>
        /// Step injector that may be promoted to become the default pipeline step injector
        /// </summary>
        class PipelineStepInjector : IPipeline
        {
            readonly ConcurrentDictionary<Type, Tuple<Position, IOutgoingStep>> _outgoingInjectedSteps = new ConcurrentDictionary<Type, Tuple<Position, IOutgoingStep>>();
            readonly ConcurrentDictionary<Type, Tuple<Position, IIncomingStep>> _incomingInjectedSteps = new ConcurrentDictionary<Type, Tuple<Position, IIncomingStep>>();
            readonly IPipeline _pipeline;

            public PipelineStepInjector(IPipeline pipeline)
            {
                _pipeline = pipeline;
            }

            public IEnumerable<StagedStep<IOutgoingStep, SendStage>> SendPipeline()
            {
                foreach (var step in _pipeline.SendPipeline())
                {
                    Tuple<Position, IOutgoingStep> injectedStep;

                    if (_outgoingInjectedSteps.TryGetValue(step.Step.GetType(), out injectedStep))
                    {
                        if (injectedStep.Item1 == Position.Before)
                        {
                            yield return new StagedStep<IOutgoingStep, SendStage>(injectedStep.Item2, SendStage.None);
                            yield return step;
                        }
                        else
                        {
                            yield return step;
                            yield return new StagedStep<IOutgoingStep, SendStage>(injectedStep.Item2, SendStage.None);
                        }
                    }
                    else
                    {
                        yield return step;
                    }
                }
            }

            public IEnumerable<StagedStep<IIncomingStep, ReceiveStage>> ReceivePipeline()
            {
                foreach (var step in _pipeline.ReceivePipeline())
                {
                    Tuple<Position, IIncomingStep> injectedStep;

                    if (_incomingInjectedSteps.TryGetValue(step.Step.GetType(), out injectedStep))
                    {
                        if (injectedStep.Item1 == Position.Before)
                        {
                            yield return new StagedStep<IIncomingStep, ReceiveStage>(injectedStep.Item2, ReceiveStage.TransportMessageReceived);
                            yield return step;
                        }
                        else
                        {
                            yield return step;
                            yield return new StagedStep<IIncomingStep, ReceiveStage>(injectedStep.Item2, ReceiveStage.TransportMessageReceived);
                        }
                    }
                    else
                    {
                        yield return step;
                    }
                }
            }

            public PipelineStepInjector OnSend(IOutgoingStep step, Position position, Type anchorStep)
            {
                _outgoingInjectedSteps[anchorStep] = Tuple.Create(position, step);
                return this;
            }

            public PipelineStepInjector OnReceive(IIncomingStep step, Position position, Type anchorStep)
            {
                _incomingInjectedSteps[anchorStep] = Tuple.Create(position, step);
                return this;
            }
        }

        enum Position
        {
            Before,
            After
        }
    }
}