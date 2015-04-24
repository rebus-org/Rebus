using System;

namespace Rebus.Sagas
{
    /// <summary>
    /// Interface of a saga data instance. The saga data represents the state of the state machine instance of the process manager,
    /// that the saga essentially implements.
    /// </summary>
    public interface ISagaData
    {
        /// <summary>
        /// Saga ID used by Rebus. Do not mess with this one.
        /// </summary>
        Guid Id { get; set; }

        /// <summary>
        /// Saga instance revision used by Rebus. Do not mess with this one.
        /// </summary>
        int Revision { get; set; }
    }
}