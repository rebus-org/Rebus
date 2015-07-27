using Rebus.Bus.Advanced;
// ReSharper disable CheckNamespace

namespace Rebus.Bus
{
    // private classes with access to private members of RebusBus
    public partial class RebusBus
    {
        class AdvancedApi : IAdvancedApi
        {
            readonly RebusBus _rebusBus;

            public AdvancedApi(RebusBus rebusBus)
            {
                _rebusBus = rebusBus;
            }

            public IWorkersApi Workers
            {
                get { return new WorkersApi(_rebusBus); }
            }
        }

        class WorkersApi : IWorkersApi
        {
            readonly RebusBus _rebusBus;

            public WorkersApi(RebusBus rebusBus)
            {
                _rebusBus = rebusBus;
            }

            public int Count
            {
                get { return _rebusBus.GetNumberOfWorkers(); }
            }

            public void AddWorker()
            {
                _rebusBus.AddWorker();
            }

            public void RemoveWorker()
            {
                _rebusBus.RemoveWorker();
            }
        }
    }
}