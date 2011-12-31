using System;

namespace Rebus
{
    public interface IMessageModule
    {
        void Before();
        void After();
        void OnError(Exception e);
    }
}