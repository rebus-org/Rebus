using System;
using System.Diagnostics;
using NUnit.Framework;
using Rebus.Logging;
using Shouldly;

namespace Rebus.Tests.Logging
{
    [TestFixture]
    public class TestRebusLoggerFactory : FixtureBase
    {
        IRebusLoggerFactory currentLogger;

        protected override void DoSetUp()
        {
            currentLogger = RebusLoggerFactory.Current;
        }

        protected override void DoTearDown()
        {
            RebusLoggerFactory.Current = currentLogger;
        }

        [Test]
        public void CanChangeLoggerAnytime()
        {
            RebusLoggerFactory.Current = new ConsoleLoggerFactory(true);

            SomeClass.Log.GetType().Name.ShouldBe("ConsoleLogger");

            RebusLoggerFactory.Current = new NullLoggerFactory();

            SomeClass.Log.GetType().Name.ShouldBe("NullLogger");
        }

        class SomeClass
        {
            public static ILog Log;

            static SomeClass()
            {
                RebusLoggerFactory.Changed += f => Log = f.GetCurrentClassLogger();
            }
        }

        [Test]
        public void CanInspectStackFrameAndDetermineCurrentClassLogger()
        {
            RebusLoggerFactory.Current = new TestLoggerFactory();

            var anotherClassLog = (TestLogger)AnotherClass.Log;
            anotherClassLog.ShouldBeOfType<TestLogger>();
            anotherClassLog.Type.ShouldBe(typeof(AnotherClass));

            var yetAnotherClassLog = (TestLogger)YetAnotherClass.Log;
            yetAnotherClassLog.ShouldBeOfType<TestLogger>();
            yetAnotherClassLog.Type.ShouldBe(typeof(YetAnotherClass));
        }

        [TestCase(10000)]
        [TestCase(100000, Ignore = true)]
        [TestCase(1000000, Ignore = true)]
        public void ComparePerformanceOfLoggerFactoryMethods(int repetitions)
        {
            var factory = new OpenConsoleLoggerFactory();

            var t1 = Stopwatch.StartNew();
            repetitions.Times(() => factory.CallGetLogger(typeof(TestRebusLoggerFactory)));
            Console.WriteLine("Old factory method: {0:0.0} s", t1.Elapsed.TotalSeconds);

            var t2 = Stopwatch.StartNew();
            repetitions.Times(() => factory.GetCurrentClassLogger());
            Console.WriteLine("New factory method: {0:0.0} s", t2.Elapsed.TotalSeconds);
        }

        class OpenConsoleLoggerFactory : ConsoleLoggerFactory
        {
            public OpenConsoleLoggerFactory() : base(true)
            {
            }

            public ILog CallGetLogger(Type type)
            {
                return GetLogger(type);
            }
        }

        class TestLoggerFactory : AbstractRebusLoggerFactory
        {
            protected override ILog GetLogger(Type type)
            {
                return new TestLogger(type);
            }
        }

        class TestLogger : ILog
        {
            readonly Type type;

            public TestLogger(Type type)
            {
                this.type = type;
            }

            public Type Type
            {
                get { return type; }
            }

            public void Debug(string message, params object[] objs)
            {
                throw new NotImplementedException();
            }

            public void Info(string message, params object[] objs)
            {
                throw new NotImplementedException();
            }

            public void Warn(string message, params object[] objs)
            {
                throw new NotImplementedException();
            }

            public void Error(Exception exception, string message, params object[] objs)
            {
                throw new NotImplementedException();
            }

            public void Error(string message, params object[] objs)
            {
                throw new NotImplementedException();
            }
        }

        class AnotherClass
        {
            public static ILog Log;

            static AnotherClass()
            {
                RebusLoggerFactory.Changed += f => Log = f.GetCurrentClassLogger();
            }
        }

        class YetAnotherClass
        {
            public static ILog Log;

            static YetAnotherClass()
            {
                RebusLoggerFactory.Changed += f => Log = f.GetCurrentClassLogger();
            }
        }
    }
}