using System;
using NUnit.Framework;
using System.Linq;
using Shouldly;

namespace Rebus.Tests.Unit
{
    [TestFixture]
    public class TestSimpleHandlerActivator : FixtureBase
    {
        SimpleHandlerActivator activator;

        protected override void DoSetUp()
        {
            activator = new SimpleHandlerActivator();
        }

        [Test]
        public void YieldsNoHandlersWhenNothingHasBeenRegistered()
        {
            // arrange
                        

            // act
            var instances = activator.GetHandlerInstancesFor<string>();

            // assert
            instances.Count().ShouldBe(0);
        }

        [Test]
        public void CanCreateHandlerFromRegisteredType()
        {
            // arrange
            activator.Register(typeof (SomeHandler));

            // act
            var instances = activator.GetHandlerInstancesFor<string>().ToList();

            // assert
            instances.Count.ShouldBe(1);
            instances[0].ShouldBeTypeOf<SomeHandler>();
        }

        [Test]
        public void WillHappilyCreateMultipleInstances()
        {
            // arrange
            activator.Register(typeof (SomeHandler))
                .Register(typeof (AnotherHandler))
                .Register(typeof (ThirdHandler));

            // act
            var stringHandlers = activator.GetHandlerInstancesFor<string>().ToList();
            var dateTimeHandlers = activator.GetHandlerInstancesFor<DateTime>().ToList();

            // assert
            stringHandlers.Count.ShouldBe(3);
            stringHandlers[0].ShouldBeTypeOf<SomeHandler>();
            stringHandlers[1].ShouldBeTypeOf<AnotherHandler>();
            stringHandlers[2].ShouldBeTypeOf<ThirdHandler>();

            dateTimeHandlers.Count.ShouldBe(1);
            dateTimeHandlers[0].ShouldBeTypeOf<ThirdHandler>();
        }

        [Test]
        public void WillCreateHandlerFromSuppliesFactoryMethodAsWell()
        {
            // arrange
            activator.Register(() => new ThirdHandler());

            // act
            var stringHandlers = activator.GetHandlerInstancesFor<string>().ToList();
            var dateTimeHandlers = activator.GetHandlerInstancesFor<DateTime>().ToList();

            // assert
            stringHandlers.Count.ShouldBe(1);
            stringHandlers[0].ShouldBeTypeOf<ThirdHandler>();

            dateTimeHandlers.Count.ShouldBe(1);
            dateTimeHandlers[0].ShouldBeTypeOf<ThirdHandler>();
        }

        class ThirdHandler : IHandleMessages<string>, IHandleMessages<DateTime>
        {
            public void Handle(string message)
            {
                throw new NotImplementedException();
            }

            public void Handle(DateTime message)
            {
                throw new NotImplementedException();
            }
        }

        class AnotherHandler : IHandleMessages<string>
        {
            public void Handle(string message)
            {
                throw new NotImplementedException();
            }
        }

        class SomeHandler : IHandleMessages<string>
        {
            public void Handle(string message)
            {
                throw new NotImplementedException();
            }
        }
    }
}