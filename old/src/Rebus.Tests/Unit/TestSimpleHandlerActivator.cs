using System;
using System.Collections.Generic;
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
        public void CanRegisterFunctionAsHandler()
        {
            // arrange
            var list = new List<string>();
            activator.Handle<string>(list.Add);

            // act
            activator.GetHandlerInstancesFor<string>()
                .OfType<IHandleMessages<string>>()
                .ToList()
                .ForEach(h => h.Handle("hello there!!"));

            // assert
            list.Count.ShouldBe(1);
            list[0].ShouldBe("hello there!!");
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
            instances[0].ShouldBeOfType<SomeHandler>();
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
            stringHandlers[0].ShouldBeOfType<SomeHandler>();
            stringHandlers[1].ShouldBeOfType<AnotherHandler>();
            stringHandlers[2].ShouldBeOfType<ThirdHandler>();

            dateTimeHandlers.Count.ShouldBe(1);
            dateTimeHandlers[0].ShouldBeOfType<ThirdHandler>();
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
            stringHandlers[0].ShouldBeOfType<ThirdHandler>();

            dateTimeHandlers.Count.ShouldBe(1);
            dateTimeHandlers[0].ShouldBeOfType<ThirdHandler>();
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