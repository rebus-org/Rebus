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
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Rebus.Configuration;
using Rhino.Mocks;
using Shouldly;

namespace Rebus.Tests.Configuration
{
    [TestFixture]
    public class TestDetermineDestinationFromNServiceBusEndpointMappings : FixtureBase
    {
        DetermineDestinationFromNServiceBusEndpointMappings mapper;
        IAppConfigLoader loader;

        protected override void DoSetUp()
        {
            loader = Mock<IAppConfigLoader>();
            mapper = new DetermineDestinationFromNServiceBusEndpointMappings(loader);
        }

        [Test]
        public void ErrorMessageInCaseOfMissingEndpointMappingIsAwesome()
        {
            StubConfig("<configuration></configuration>");

            var exception = Assert.Throws<InvalidOperationException>(() => mapper.GetEndpointFor(typeof(DateTime)));

            var errorMessage = exception.Message;

            Console.WriteLine(errorMessage);

            errorMessage.ShouldContain("System.DateTime");
            errorMessage.ShouldContain("UnicastBusConfig");
            errorMessage.ShouldContain("MessageEndpointMappings");
        }

        [Test]
        public void ThrowsIfMappingCannotBeFound()
        {
            StubConfig("<configuration></configuration>");

            Assert.Throws<InvalidOperationException>(() => mapper.GetEndpointFor(typeof (string)));
        }

        [TestCase(@"<configuration></configuration>")]
        [TestCase(@"<configuration>
  <UnicastBusConfig>
    <MessageEndpointMappings>
    </MessageEndpointMappings>
  </UnicastBusConfig>
</configuration>")]
        [TestCase(@"<configuration>
  <UnicastBusConfig>
    <MessageEndpointMappWFJI>
    </MessageEndpointMappWFJI>
  </UnicastBusConfig>
</configuration>")]
        [TestCase(@"<configuration>
  <UnicastBusConfi>
    <MessageEndpointMappings>
    </MessageEndpointMappings>
  </UnicastBusConfi>
</configuration>")]
        public void DoesNotChokeOnInvalidOrEmptyAppConfig(string appConfigText)
        {
            // arrange
            StubConfig(appConfigText);
        }

        [Test]
        public void CanDetermineDestinationByLookingAtAssembly()
        {
            // arrange
            StubConfigFile("app.config.01.txt");

            // act
            // assert
            mapper.GetEndpointFor(typeof(InnerClass)).ShouldBe("this_is_just_some_endpoint");
            mapper.GetEndpointFor(typeof(OuterClass)).ShouldBe("this_is_just_some_endpoint");
        }

        [Test]
        public void CanDetermineDestinationByLookingAtAssemblyQualifiedName()
        {
            // arrange
            StubConfigFile("app.config.02.txt");

            // act
            // assert
            mapper.GetEndpointFor(typeof(InnerClass)).ShouldBe("inner_class_endpoint");
            mapper.GetEndpointFor(typeof(OuterClass)).ShouldBe("outer_class_endpoint");
        }

        [Test]
        public void ImplementationIfMeantToBeCustomizedBySubclassing()
        {
            mapper.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(m => m.DeclaringType == typeof(DetermineDestinationFromNServiceBusEndpointMappings))
                .ToList()
                .ForEach(AssertIsProtectedVirtual);
        }

        void AssertIsProtectedVirtual(MethodInfo methodInfo)
        {
            Assert.IsTrue(methodInfo.IsVirtual,
                          "Impl is meant to be overridable, so all non-public methods should be virtual! {0}",
                          methodInfo);

            Assert.IsFalse(methodInfo.IsPrivate,
                          "Impl is meant to be overridable, so all non-public methods must be protected! {0}",
                          methodInfo);
        }

        void StubConfigFile(string appConfigFileName)
        {
            StubConfig(File.ReadAllText(Path.Combine("Configuration", "AppConfigExamples", appConfigFileName)));
        }

        void StubConfig(string appConfigText)
        {
            loader.Stub(a => a.LoadIt()).Return(appConfigText);
        }

        class InnerClass {}
    }

    class OuterClass {}
}