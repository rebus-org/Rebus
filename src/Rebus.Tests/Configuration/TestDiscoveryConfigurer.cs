// Copyright 2011 Mogens Heller Grabe
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software distributed under the License is
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.
using System;
using System.Reflection;
using NUnit.Framework;
using Rebus.Configuration.Configurers;
using Rhino.Mocks;
using Shouldly;

namespace Rebus.Tests.Configuration
{
    [TestFixture]
    public class TestDiscoveryConfigurer : FixtureBase
    {
        IContainerAdapter containerAdapter;
        DiscoveryConfigurer configurer;

        protected override void DoSetUp()
        {
            containerAdapter = Mock<IContainerAdapter>();
            configurer = new DiscoveryConfigurer(containerAdapter);
        }

        [Test]
        public void CanDiscoverHandlers()
        {
            // arrange

            // act
            configurer.Handlers.LoadFrom(Assembly.GetExecutingAssembly());

            // assert
            containerAdapter.AssertWasCalled(c => c.Register(typeof (ThisClassNameIsPrettyRecognizable),
                                                             Lifestyle.Instance,
                                                             typeof (IHandleMessages<string>)));
        }

        [Test]
        public void CanFilterHandlers()
        {
            // arrange
            var wasCalled = false;

            // act
            configurer.Handlers.LoadFrom(t =>
                                             {
                                                 if (t == typeof (ThisClassNameIsPrettyRecognizable))
                                                 {
                                                     wasCalled = true;
                                                     return false;
                                                 }
                                                 return true;
                                             }, Assembly.GetExecutingAssembly());

            // assert
            containerAdapter
                .AssertWasNotCalled(c => c.Register(Arg<Type>.Is.Equal(typeof (ThisClassNameIsPrettyRecognizable)),
                                                    Arg<Lifestyle>.Is.Anything,
                                                    Arg<Type[]>.Is.Anything));

            wasCalled.ShouldBe(true);
        }

        class ThisClassNameIsPrettyRecognizable : IHandleMessages<string>
        {
            public void Handle(string message)
            {

            }
        }
    }
}