using System.Threading.Tasks;

namespace Rebus.Handlers
{
    public interface IHandleMessages { }
    
    public interface IHandleMessages<TMessage> : IHandleMessages
    {
        Task Handle(TMessage message);
    }
}