using System;

namespace Rebus
{
    public interface ISagaData
    {
        Guid Id { get; }
    }
}