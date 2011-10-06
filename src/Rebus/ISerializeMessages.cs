namespace Rebus
{
    /// <summary>
    /// Implement this to specify how messages are represented as strings.
    /// </summary>
    public interface ISerializeMessages
    {
        string Serialize(object obj);
        object Deserialize(string str);
    }
}