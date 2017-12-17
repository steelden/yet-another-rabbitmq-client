using System;

namespace YARC.Utility.Serialization
{
    public interface IDataSerializationStrategy
    {
        string ConvertFromBytes(byte[] bytes);
        byte[] ConvertToBytes(string text);

        Tuple<string, byte[]> Serialize(object message);
        Tuple<Type, object> Deserialize(string typeName, byte[] bytes);
        T Deserialize<T>(byte[] bytes);

        Tuple<string, string> SerializeString(object message);
        Tuple<Type, object> DeserializeString(string typeName, string text);
        T DeserializeString<T>(string text);
    }
}
