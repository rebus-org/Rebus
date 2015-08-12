using System;
using NUnit.Framework;
using Rebus.Logging;
using Rebus.Testing;
using Rhino.Mocks;
using log4net.Config;

namespace Rebus.Tests
{
    public abstract class FixtureBase
    {
        static FixtureBase()
        {
            XmlConfigurator.Configure();
        }

        [SetUp]
        public void SetUp()
        {
            Console.WriteLine("---BEGIN SETUP---------------------------------------------");
            TimeMachine.Reset();
            FakeMessageContext.Reset();
            RebusLoggerFactory.Reset();
            DoSetUp();
            Console.WriteLine("---DONE SETTING UP-----------------------------------------");
        }

        protected virtual void DoSetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
            Console.WriteLine("---BEGIN TEARDOWN------------------------------------------");
            DoTearDown();
            CleanUpTrackedDisposables();
            Console.WriteLine("---DONE TEARING DOWN---------------------------------------");
        }

        protected T TrackDisposable<T>(T instanceToTrack) where T : IDisposable
        {
            DisposableTracker.TrackDisposable(instanceToTrack);
            return instanceToTrack;
        }

        protected void CleanUpTrackedDisposables()
        {
            DisposableTracker.DisposeTheDisposables();
        }

        protected virtual void DoTearDown()
        {
        }

        protected T Mock<T>() where T : class
        {
            return MockRepository.GenerateMock<T>();
        }
    }
}