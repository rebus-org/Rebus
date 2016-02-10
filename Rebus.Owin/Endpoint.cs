using System;
using Microsoft.Owin.Hosting;
using Owin;
using Rebus.Exceptions;
using Rebus.Logging;

namespace Rebus.Owin
{
    class Endpoint : IDisposable
    {
        readonly ILog _logger;
        readonly string _listenUrl;
        readonly Action<IAppBuilder> _startup;

        IDisposable _host;

        public Endpoint(ILog logger, string listenUrl, Action<IAppBuilder> startup)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (listenUrl == null) throw new ArgumentNullException(nameof(listenUrl));
            if (startup == null) throw new ArgumentNullException(nameof(startup));
            _logger = logger;
            _listenUrl = listenUrl;
            _startup = startup;
        }


        public void Start()
        {
            _logger.Info($"Starting web host listening on {_listenUrl}");

            try
            {
                _host = WebApp.Start(_listenUrl, _startup);
            }
            catch (Exception exception)
            {
                throw new RebusApplicationException(exception, $"Could not open web host on {_listenUrl}");
            }
        }

        public void Dispose()
        {
            _logger.Info($"Stopping web host listening on {_listenUrl}");
            _host?.Dispose();
            _host = null;
        }
    }
}