using System.Threading;
using System.Transactions;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.Shared;
using Rebus.Transports.Msmq;
using Shouldly;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestConfigurableTransactionScope : RebusBusMsmqIntegrationTestBase
    {
        const string InputQueueName = "test.txscope.input";
        const string ErrorQueueName = "error";

        protected override void DoSetUp()
        {
            MsmqUtil.Delete(InputQueueName);
            MsmqUtil.Delete(ErrorQueueName);
        }

        protected override void DoTearDown()
        {
            MsmqUtil.Delete(InputQueueName);
            MsmqUtil.Delete(ErrorQueueName);
        }

        [Test]
        public void DoesNotUseTransactionScopeByDefault()
        {
            using (var adapter = new BuiltinContainerAdapter())
            {
                var resetEvent = new ManualResetEvent(false);
                var transactionScopeDetected = false;

                adapter.Handle<string>(str =>
                    {
                        transactionScopeDetected = Transaction.Current != null;
                        resetEvent.Set();
                    });

                Configure.With(adapter)
                         .Transport(t => t.UseMsmq(InputQueueName, ErrorQueueName))
                         .CreateBus()
                         .Start();

                adapter.Bus.SendLocal("bla bla whatever");

                if (!resetEvent.WaitOne(2.Seconds()))
                {
                    Assert.Fail("Did not receive the message withing 2 second timeout!!");
                }

                transactionScopeDetected.ShouldBe(false);
            }
        }

        [Test]
        public void CanBeConfiguredToHandleMessagesInsideTransactionScope()
        {
            using (var adapter = new BuiltinContainerAdapter())
            {
                var resetEvent = new ManualResetEvent(false);
                var transactionScopeDetected = false;

                adapter.Handle<string>(str =>
                    {
                        transactionScopeDetected = Transaction.Current != null;
                        resetEvent.Set();
                    });

                Configure.With(adapter)
                         .Transport(t => t.UseMsmq(InputQueueName, ErrorQueueName))
                         .Behavior(b => b.HandleMessagesInsideTransactionScope())
                         .CreateBus()
                         .Start();

                adapter.Bus.SendLocal("bla bla whatever");

                if (!resetEvent.WaitOne(2.Seconds()))
                {
                    Assert.Fail("Did not receive the message withing 2 second timeout!!");
                }

                transactionScopeDetected.ShouldBe(true);
            }
        }
    }
}