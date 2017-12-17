using System;

namespace YARC.Utility.Serialization
{
    public class SimpleTypeSerializationStrategy : ITypeSerializationStrategy
    {
        public string Serialize(Type type)
        {
            return type.FullName + ":" + type.Assembly.GetName().Name;
        }

        public Type Deserialize(string typeName)
        {
            var parts = typeName.Split(':');
            if (parts.Length == 2)
            {
                string fullName = string.Format("{0}, {1}", parts[0], parts[1]);
                return Type.GetType(fullName, false);
            }
            return null;
        }
    }
}
