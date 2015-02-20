using System.Threading.Tasks;

namespace Rebus2.Handlers
{
    public interface IHandleMessages<TMessage>
    {
        Task Handle(TMessage message);
    }
}