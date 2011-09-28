using System;

namespace Rebus
{
    public interface IStoreSubscriptions
    {
        void Save(Type messageType, string subscriberInputQueue);
        string[] GetSubscribers(Type messageType);
    }
}