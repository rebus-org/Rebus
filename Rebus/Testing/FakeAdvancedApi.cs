using System;
using Rebus.Bus.Advanced;
using Rebus.DataBus;

namespace Rebus.Testing
{
    /// <summary>
    /// Implementation of <see cref="IAdvancedApi"/> that can be used to set up fake implementations of various advanced bus APIs for running isolated tests
    /// </summary>
    public class FakeAdvancedApi : IAdvancedApi
    {
        readonly IWorkersApi _workers;
        readonly ITopicsApi _topics;
        readonly IRoutingApi _routing;
        readonly ITransportMessageApi _transportMessage;
        readonly IDataBus _dataBus;

        /// <summary>
        /// Creates the fake advanced API, using the given implementation(s). All arguments are optional
        /// </summary>
        public FakeAdvancedApi(IWorkersApi workers = null, ITopicsApi topics = null, IRoutingApi routing = null, ITransportMessageApi transportMessage = null, IDataBus dataBus = null)
        {
            _workers = workers;
            _topics = topics;
            _routing = routing;
            _transportMessage = transportMessage;
            _dataBus = dataBus;
        }

        /// <summary>
        /// Gets the workers API if one was passed to the constructor, or throws an exception if that is not the case
        /// </summary>
        public IWorkersApi Workers
        {
            get
            {
                if (_workers != null) return _workers;

                throw new InvalidOperationException("No IWorkersApi implementation was passed to the FakeAdvancedApi upon construction. Please pass an implementation if you would like to be able to run isolated tests against the workers API");
            }
        }

        /// <summary>
        /// Gets the topics API if one was passed to the constructor, or throws an exception if that is not the case
        /// </summary>
        public ITopicsApi Topics
        {
            get
            {
                if (_topics != null) return _topics;

                throw new InvalidOperationException("No ITopicsApi implementation was passed to the FakeAdvancedApi upon construction. Please pass an implementation if you would like to be able to run isolated tests against the topics API");
            }
        }

        /// <summary>
        /// Gets the routing API if one was passed to the constructor, or throws an exception if that is not the case
        /// </summary>
        public IRoutingApi Routing
        {
            get
            {
                if (_routing != null) return _routing;

                throw new InvalidOperationException("No IRoutingApi implementation was passed to the FakeAdvancedApi upon construction. Please pass an implementation if you would like to be able to run isolated tests against the routing API");
            }
        }

        /// <summary>
        /// Gets the transport message API if one was passed to the constructor, or throws an exception if that is not the case
        /// </summary>
        public ITransportMessageApi TransportMessage
        {
            get
            {
                if (_transportMessage != null) return _transportMessage;

                throw new InvalidOperationException("No ITransportMessageApi implementation was passed to the FakeAdvancedApi upon construction. Please pass an implementation if you would like to be able to run isolated tests against the transport message API");
            }
        }

        /// <summary>
        /// Gets the data bus API if one was passed to the constructor, or throws an exception if that is not the case
        /// </summary>
        public IDataBus DataBus
        {
            get
            {
                if (_dataBus != null) return _dataBus;

                throw new InvalidOperationException("No IDataBusApi implementation was passed to the FakeAdvancedApi upon construction. Please pass an implementation if you would like to be able to run isolated tests against the data bus API");
            }
        }
    }
}