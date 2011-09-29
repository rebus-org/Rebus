namespace Rebus
{
    public interface IHandleMessages<in T>
    {
        void Handle(T message);
    }
}