using System;
using System.Linq;
using System.Threading;
using Rebus2.Logging;
using Rebus2.Pipeline;
using Rebus2.Transport;

namespace Rebus2.Bus
{
    public class Worker : IDisposable
    {
        static ILog _log;

        static Worker()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        readonly ITransport _transport;
        readonly IPipeline _pipeline;
        readonly Thread _workerThread;
        readonly PipelineInvoker _pipelineInvoker = new PipelineInvoker();

        volatile bool _keepWorking = true;

        public Worker(ITransport transport, IPipeline pipeline, string workerName)
        {
            _transport = transport;
            _pipeline = pipeline;
            _workerThread = new Thread(() =>
            {
                while (_keepWorking)
                {
                    try
                    {
                        DoWork();
                    }
                    catch (Exception exception)
                    {
                        _log.Error(exception, "Error while attempting to do work");
                    }
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
            using (var transactionContext = new DefaultTransactionContext())
            {
                AmbientTransactionContext.Current = transactionContext;

                var message = _transport.Receive(transactionContext).Result;

                if (message == null)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(0.5));
                    return;
                }

                var context = new StepContext(message);
                transactionContext.Items[StepContext.StepContextKey] = context;
                
                var stagedReceiveSteps = _pipeline.ReceivePipeline();
                _pipelineInvoker.Invoke(context, stagedReceiveSteps.Select(s => s.Step));
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