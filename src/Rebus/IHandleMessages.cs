namespace Rebus
{
    public interface IHandleMessages<T>
    {
        void Handle(T message);
    }
}