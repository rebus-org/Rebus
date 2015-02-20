using System;
using System.Threading;
using System.Threading.Tasks;
using Rebus2.Activation;
using Rebus2.Dispatch;
using Rebus2.Messages;
using Rebus2.Serialization;
using Rebus2.Transport;

namespace Rebus2.Bus
{
    public class Worker : IDisposable
    {
        readonly IHandlerActivator _handlerActivator;
        readonly ITransport _transport;
        readonly ISerializer _serializer;
        readonly Thread _workerThread;
        readonly Dispatcher _dispatcher;

        volatile bool _keepWorking;

        public Worker(IHandlerActivator handlerActivator, ITransport transport, ISerializer serializer)
        {
            _handlerActivator = handlerActivator;
            _transport = transport;
            _serializer = serializer;
            _dispatcher = new Dispatcher();
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
                        
                    }
                }
            });
            _workerThread.Start();
        }

        void DoWork()
        {
            try
            {
                using (var transactionContext = new DefaultTransactionContext())
                {
                    var message = _transport.Receive(transactionContext).Result;

                    if (message == null)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(0.5));
                        return;
                    }

                    _dispatcher.Dispatch(message).Wait();
                }
            }
            catch (Exception exception)
            {
                
            }
        }

        public void Dispose()
        {
            _keepWorking = false;
            _workerThread.Join();
        }
    }
}