using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus2.Activation;
using Rebus2.Extensions;
using Rebus2.Messages;
using Rebus2.Routing;
using Rebus2.Serialization;
using Rebus2.Transport;

namespace Rebus2.Bus
{
    public class RebusBus : IDisposable
    {
        readonly List<Worker> _workers = new List<Worker>();
        readonly IHandlerActivator _handlerActivator;
        readonly IRouter _router;
        readonly ITransport _transport;
        readonly ISerializer _serializer;

        public RebusBus(IHandlerActivator handlerActivator, IRouter router, ITransport transport, ISerializer serializer)
        {
            // we do not control the lifetime of the handler activator - it controls us!
            _handlerActivator = handlerActivator;
            _router = router;
            _transport = transport;
            _serializer = serializer;
        }

        public void Start()
        {
            InjectedServicesWhoseLifetimeToControl
                .OfType<IInitializable>()
                .ForEach(i => i.Initialize());

            SetNumberOfWorkers(10);
        }

        IEnumerable InjectedServicesWhoseLifetimeToControl
        {
            get
            {
                yield return _router;
                yield return _transport;
                yield return _serializer;
            }
        } 

        public async Task Send(object message)
        {
            var destinationAddress = _router.GetDestinationAddress(message);

            var headers = new Dictionary<string, string>();
            var transportMessage = await _serializer.Serialize(new Message(headers, message));
            
            using (var defaultTransactionContext = new DefaultTransactionContext())
            {
                await _transport.Send(destinationAddress, transportMessage, defaultTransactionContext);
                defaultTransactionContext.Commit();
            }
        }

        ~RebusBus()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                
            }

            SetNumberOfWorkers(0);
        }

        void SetNumberOfWorkers(int desiredNumberOfWorkers)
        {
            while (desiredNumberOfWorkers > _workers.Count) AddWorker();
            while (desiredNumberOfWorkers < _workers.Count) RemoveWorker();
        }

        void AddWorker()
        {
            lock (_workers)
            {
                _workers.Add(new Worker(_handlerActivator, _transport, _serializer));
            }
        }

        void RemoveWorker()
        {
            lock (_workers)
            {
                if (_workers.Count == 0) return;

                using (var lastWorker = _workers.Last())
                {
                    _workers.Remove(lastWorker);
                }
            }
            
        }
    }
}