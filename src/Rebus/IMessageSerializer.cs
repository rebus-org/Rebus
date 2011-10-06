namespace Rebus
{
    public interface IMessageSerializer
    {
        string Serialize(object obj);
        object Deserialize(string str);
    }
}