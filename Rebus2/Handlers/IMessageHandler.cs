using System.Threading.Tasks;

namespace Rebus2.Handlers
{
    public interface IHandleMessages { }
    
    public interface IHandleMessages<TMessage> : IHandleMessages
    {
        Task Handle(TMessage message);
    }
}