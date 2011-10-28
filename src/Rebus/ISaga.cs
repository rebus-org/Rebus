using System;
using System.Collections.Concurrent;

namespace Rebus
{
    public interface ISaga
    {
        ConcurrentDictionary<Type, Correlation> Correlations { get; }
        void ConfigureHowToFindSaga();
    }

    public interface ISaga<TData> : ISaga
    {
    }
}