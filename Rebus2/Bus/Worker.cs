using System;
using System.Threading;
using Rebus2.Activation;
using Rebus2.Dispatch;
using Rebus2.Extensions;
using Rebus2.Logging;
using Rebus2.Messages;
using Rebus2.Serialization;
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
        readonly Thread _workerThread;
        readonly Dispatcher _dispatcher;

        volatile bool _keepWorking = true;

        public Worker(IHandlerActivator handlerActivator, ITransport transport, ISerializer serializer, string workerName)
        {
            _transport = transport;
            _dispatcher = new Dispatcher(handlerActivator, serializer);
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
                var message = _transport.Receive(transactionContext).Result;

                if (message == null)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(0.5));
                    return;
                }

                _log.Debug("Received message {0}", message.Headers.GetValueOrNull(Headers.MessageId) ?? "<no ID>");
                _dispatcher.Dispatch(message).Wait();
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