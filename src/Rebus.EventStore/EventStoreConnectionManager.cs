using System;
using System.Net;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace Rebus.EventStore
{
    class EventStoreConnectionManager
    {
        private static volatile IEventStoreConnection connection;
        private static readonly object SyncRoot = new Object();

        private EventStoreConnectionManager() { }

        public static IEventStoreConnection CreateConnectionAndWait()
        {
            if (connection == null)
            {
                lock (SyncRoot)
                {
                    if (connection == null)
                    {
                        var settings = ConnectionSettings.Create();
                        settings.SetDefaultUserCredentials(new UserCredentials("admin", "changeit"));
                        connection = EventStoreConnection.Create(settings, new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1113));
                        connection.ConnectAsync().Wait();
                    }
                }
            }

            return connection;
        }

    }
}
