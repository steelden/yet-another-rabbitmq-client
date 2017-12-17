using System;

namespace YARC.Utility.Serialization
{
    public interface ITypeSerializationStrategy
    {
        string Serialize(Type type);
        Type Deserialize(string typeName);
    }
}
