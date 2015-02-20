using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus2.Activation;
using Rebus2.Extensions;
using Rebus2.Logging;
using Rebus2.Messages;
using Rebus2.Pipeline;
using Rebus2.Routing;
using Rebus2.Serialization;
using Rebus2.Transport;

namespace Rebus2.Bus
{
    public class RebusBus : IDisposable
    {
        static ILog _log;

        static RebusBus()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        readonly List<Worker> _workers = new List<Worker>();
        readonly IHandlerActivator _handlerActivator;
        readonly IRouter _router;
        readonly ITransport _transport;
        readonly ISerializer _serializer;
        readonly IPipelineManager _pipelineManager;

        public RebusBus(IHandlerActivator handlerActivator, IRouter router, ITransport transport, ISerializer serializer, IPipelineManager pipelineManager)
        {
            // we do not control the lifetime of the handler activator - it controls us!
            _handlerActivator = handlerActivator;
            _router = router;
            _transport = transport;
            _serializer = serializer;
            _pipelineManager = pipelineManager;
        }

        public void Start()
        {
            Start(1);
        }

        public void Start(int numberOfWorkers)
        {
            _log.Info("Starting bus");

            InjectedServicesWhoseLifetimeToControl
                .OfType<IInitializable>()
                .ForEach(i =>
                {
                    _log.Debug("Initializing {0}", i);
                    i.Initialize();
                });

            SetNumberOfWorkers(5);

            _log.Info("Started");
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
            var headers = new Dictionary<string, string>();
            var logicalMessage = new Message(headers, message);
            var destinationAddress = _router.GetDestinationAddress(logicalMessage);

            var transportMessage = await _serializer.Serialize(logicalMessage);

            using (var defaultTransactionContext = new DefaultTransactionContext())
            {
                await _transport.Send(destinationAddress, transportMessage, defaultTransactionContext);
                defaultTransactionContext.Commit();
            }
        }

        public async Task Reply(object message)
        {
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

            // signal to all the workers that they must stop
            lock (_workers)
            {
                _workers.ForEach(w => w.Stop());
            }

            SetNumberOfWorkers(0);
        }

        void SetNumberOfWorkers(int desiredNumberOfWorkers)
        {
            _log.Info("Setting number of workers to {0}", desiredNumberOfWorkers);
            while (desiredNumberOfWorkers > _workers.Count) AddWorker();
            while (desiredNumberOfWorkers < _workers.Count) RemoveWorker();
        }

        void AddWorker()
        {
            lock (_workers)
            {
                var workerName = string.Format("Rebus worker {0}", _workers.Count + 1);
                _log.Debug("Adding worker {0}", workerName);
                _workers.Add(new Worker(_handlerActivator, _transport, _pipelineManager, workerName));
            }
        }

        void RemoveWorker()
        {
            lock (_workers)
            {
                if (_workers.Count == 0) return;

                _log.Debug("Removing worker");
             
                using (var lastWorker = _workers.Last())
                {
                    _workers.Remove(lastWorker);
                }
            }

        }
    }
}