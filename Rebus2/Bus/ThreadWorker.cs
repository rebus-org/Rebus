using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rebus2.Logging;
using Rebus2.Pipeline;
using Rebus2.Transport;

namespace Rebus2.Bus
{
    public class ThreadWorker : IWorker
    {
        static ILog _log;

        static ThreadWorker()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        readonly ITransport _transport;
        readonly IPipeline _pipeline;
        readonly ThreadWorkerSynchronizationContext _threadWorkerSynchronizationContext;
        readonly Thread _workerThread;
        readonly PipelineInvoker _pipelineInvoker = new PipelineInvoker();

        volatile bool _keepWorking = true;

        public ThreadWorker(ITransport transport, IPipeline pipeline, string workerName, ThreadWorkerSynchronizationContext threadWorkerSynchronizationContext)
        {
            _transport = transport;
            _pipeline = pipeline;
            _threadWorkerSynchronizationContext = threadWorkerSynchronizationContext;
            _workerThread = new Thread(() =>
            {
                SynchronizationContext.SetSynchronizationContext(_threadWorkerSynchronizationContext);

                while (_keepWorking)
                {
                    DoWork();
                }
            })
            {
                Name = workerName
            };
            _log.Debug("Starting worker {0}", workerName);
            _workerThread.Start();
        }

        void DoWork()
        {
            try
            {
                var nextContinuationOrNull = _threadWorkerSynchronizationContext.GetNextContinuationOrNull();

                if (nextContinuationOrNull != null)
                {
                    nextContinuationOrNull();
                    return;
                }

                TryProcessMessage();
            }
            catch (Exception exception)
            {
                _log.Error(exception, "Error while attempting to do work");
            }
        }

        async void TryProcessMessage()
        {
            using (var transactionContext = new DefaultTransactionContext())
            {
                try
                {
                    AmbientTransactionContext.Current = transactionContext;

                    var message = await _transport.Receive(transactionContext);

                    if (message == null)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(0.5));
                        return;
                    }

                    var context = new StepContext(message);
                    transactionContext.Items[StepContext.StepContextKey] = context;

                    var stagedReceiveSteps = _pipeline.ReceivePipeline();
                    await _pipelineInvoker.Invoke(context, stagedReceiveSteps.Select(s => s.Step));

                    transactionContext.Commit();
                }
                finally
                {
                    //AmbientTransactionContext.Current = null;
                }
            }
        }

        public void Stop()
        {
            _keepWorking = false;
        }

        public void Dispose()
        {
            _keepWorking = false;
            _workerThread.Join();
        }
    }
}