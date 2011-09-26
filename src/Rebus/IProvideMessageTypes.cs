using System;

namespace Rebus
{
    public interface IProvideMessageTypes
    {
        Type[] GetMessageTypes();
    }
}