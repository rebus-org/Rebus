using System;
using System.Linq;
using System.Threading;
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

        readonly ThreadWorkerSynchronizationContext _threadWorkerSynchronizationContext;
        readonly int _maxParallelismPerWorker;
        readonly ITransport _transport;
        readonly IPipeline _pipeline;
        readonly Thread _workerThread;
        readonly IPipelineInvoker _pipelineInvoker;

        volatile bool _keepWorking = true;

        public ThreadWorker(ITransport transport, IPipeline pipeline, IPipelineInvoker pipelineInvoker, string workerName, ThreadWorkerSynchronizationContext threadWorkerSynchronizationContext, int maxParallelismPerWorker)
        {
            _transport = transport;
            _pipeline = pipeline;
            _pipelineInvoker = pipelineInvoker;
            _threadWorkerSynchronizationContext = threadWorkerSynchronizationContext;
            _maxParallelismPerWorker = maxParallelismPerWorker;
            _workerThread = new Thread(() =>
            {
                try
                {
                    SynchronizationContext.SetSynchronizationContext(_threadWorkerSynchronizationContext);

                    while (_keepWorking)
                    {
                        DoWork();
                    }
                }
                catch (ThreadAbortException)
                {
                    _log.Info("Worker {0} aborted", workerName);
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

        int _continuationsWaitingToBePosted;

        async void TryProcessMessage()
        {
            if (_continuationsWaitingToBePosted >= _maxParallelismPerWorker)
            {
                Thread.Sleep(100);
                return;
            }

            _continuationsWaitingToBePosted++;

            using (var transactionContext = new DefaultTransactionContext())
            {
                try
                {
                    AmbientTransactionContext.Current = transactionContext;

                    var message = await _transport.Receive(transactionContext);

                    if (message == null) return;

                    var context = new IncomingStepContext(message, transactionContext);
                    transactionContext.Items[StepContext.StepContextKey] = context;

                    var stagedReceiveSteps = _pipeline.ReceivePipeline();
                    
                    await _pipelineInvoker.Invoke(context, stagedReceiveSteps.Select(s => s.Step));

                    transactionContext.Complete();
                }
                catch (Exception exception)
                {
                    _log.Error(exception, "Unhandled exception in thread worker");
                }
                finally
                {
                    AmbientTransactionContext.Current = null;
                    _continuationsWaitingToBePosted--;
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