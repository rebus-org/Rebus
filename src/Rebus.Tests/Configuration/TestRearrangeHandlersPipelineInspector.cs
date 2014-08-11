using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using Rebus.Configuration;
using System.Linq;
using Shouldly;

namespace Rebus.Tests.Configuration
{
    [TestFixture]
    public class TestRearrangeHandlersPipelineInspector : FixtureBase
    {
        RearrangeHandlersPipelineInspector inspector;

        protected override void DoSetUp()
        {
            inspector = new RearrangeHandlersPipelineInspector();
        }

        [Test]
        public void ThrowsIfSameHandlerIsSpecifiedTwice()
        {
            // arrange
            inspector.AddToOrder(typeof(FirstHandler));

            // act

            // assert
            Assert.Throws<InvalidOperationException>(() => inspector.AddToOrder(typeof (FirstHandler)));
        }

        [Test]
        public void WorksIncrementallyAsWell()
        {
            // arrange
            inspector.AddToOrder(typeof(FirstHandler));
            inspector.AddToOrder(typeof(SecondHandler));
            inspector.AddToOrder(typeof(ThirdHandler));

            // act
            var order = inspector.GetOrder();

            // assert
            order[0].ShouldBe(typeof(FirstHandler));
            order[1].ShouldBe(typeof(SecondHandler));
            order[2].ShouldBe(typeof(ThirdHandler));
        }

        /// <summary>
        /// Initial:
        ///     1000 iterations ordering 130:13 handlers took 0,14 s
        ///     1000 iterations ordering 1300:13 handlers took 1,48 s
        /// 
        /// 
        /// </summary>
        [TestCase(1000, 10)]
        [TestCase(1000, 100)]
        public void PerformsQuiteWell(int iterations, int handlerListMultiplicationFactor)
        {
            var instanceOfEachHandler = Handlers(new AuthenticationHandler(),
                                                 new FirstHandler(),
                                                 new SecondHandler(),
                                                 new ThirdHandler(),
                                                 new FourthHandler(),
                                                 new FifthHandler(),
                                                 new SixthHandler(),
                                                 new SeventhHandler(),
                                                 new EighthHandler(),
                                                 new NinthHandler(),
                                                 new TenthHandler(),
                                                 new EleventhHandler(),
                                                 new TwelfthHandler());

            var listOfHandlers = Enumerable.Repeat(instanceOfEachHandler, handlerListMultiplicationFactor)
                .Aggregate((s1, s2) => s1.Concat(s2))
                .ToList();

            var orderedTypes = new[]
                                   {
                                       typeof (AuthenticationHandler),
                                       typeof (FirstHandler),
                                       typeof (SecondHandler),
                                       typeof (ThirdHandler),
                                       typeof (FourthHandler),
                                       typeof (FifthHandler),
                                       typeof (SixthHandler),
                                       typeof (SeventhHandler),
                                       typeof (EighthHandler),
                                       typeof (NinthHandler),
                                       typeof (TenthHandler),
                                       typeof (EleventhHandler),
                                       typeof (TwelfthHandler)
                                   };
            
            inspector.SetOrder(orderedTypes);

            var stopwatch = Stopwatch.StartNew();
            iterations.Times(() => inspector.Filter(new SomeMessage(), listOfHandlers));
            Console.WriteLine("{0} iterations ordering {1}:{2} handlers took {3:0.00} s",
                              iterations,
                              listOfHandlers.Count(),
                              orderedTypes.Length,
                              stopwatch.Elapsed.TotalSeconds);
        }

        [Test]
        public void DoesNothingToHandlerTypesThatAreUnknownToTheInspector()
        {
            // arrange
            var listOfHandlers = Handlers(new FirstHandler(), new SecondHandler());
            
            // act
            var pipeline = inspector.Filter(new SomeMessage(), listOfHandlers).ToArray();
            
            // assert
            pipeline[0].ShouldBeOfType<FirstHandler>();
            pipeline[1].ShouldBeOfType<SecondHandler>();
        }

        [Test]
        public void ComplainsIfOrderHasAlreadyBeenSet()
        {
            // arrange
            inspector.SetOrder(typeof(FirstHandler));

            // act
            // assert
            Assert.Throws<InvalidOperationException>(() => inspector.SetOrder(typeof(SecondHandler)));
        }

        [Test]
        public void CanEnsureThatSomeHandlerIsExecutedFirst()
        {
            // arrange
            inspector.SetOrder(typeof (AuthenticationHandler),
                               typeof (FirstHandler),
                               typeof (SecondHandler));

            var listOfHandlers = Handlers(new ThirdHandler(), new SecondHandler(), new FirstHandler(),
                                          new AuthenticationHandler(),
                                          new FourthHandler());

            // act
            var pipeline = inspector.Filter(new SomeMessage(), listOfHandlers).ToArray();

            // assert
            pipeline[0].ShouldBeOfType<AuthenticationHandler>();
            pipeline[1].ShouldBeOfType<FirstHandler>();
            pipeline[2].ShouldBeOfType<SecondHandler>();
        }

        static IEnumerable<IHandleMessages<SomeMessage>> Handlers(params IHandleMessages<SomeMessage> [] handlers)
        {
            return handlers;
        }

        class SomeMessage { }
        abstract class HandlerOf<TMessage> : IHandleMessages<TMessage>
        {
            public void Handle(TMessage message)
            {
            }
        }

        class FirstHandler : HandlerOf<SomeMessage> { }
        class SecondHandler : HandlerOf<SomeMessage> { }
        class ThirdHandler : HandlerOf<SomeMessage> { }
        class FourthHandler : HandlerOf<SomeMessage> { }
        class FifthHandler : HandlerOf<SomeMessage> { }
        class SixthHandler : HandlerOf<SomeMessage> { }
        class SeventhHandler : HandlerOf<SomeMessage> { }
        class EighthHandler : HandlerOf<SomeMessage> { }
        class NinthHandler : HandlerOf<SomeMessage> { }
        class TenthHandler : HandlerOf<SomeMessage> { }
        class EleventhHandler : HandlerOf<SomeMessage> { }
        class TwelfthHandler : HandlerOf<SomeMessage> { }
        class AuthenticationHandler : HandlerOf<SomeMessage> { }
    }
}