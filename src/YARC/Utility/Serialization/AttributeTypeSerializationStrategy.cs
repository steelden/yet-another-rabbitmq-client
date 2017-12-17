using System;
using System.Collections.Generic;
using YARC.Messages.Attributes;

namespace YARC.Utility.Serialization
{
    public class AttributeTypeSerializationStrategy : ITypeSerializationStrategy
    {
        private readonly SimpleTypeSerializationStrategy _fallback = new SimpleTypeSerializationStrategy();

        private IDictionary<Type, string> _sregistry = new Dictionary<Type, string>();
        private IDictionary<string, Type> _dregistry = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        public AttributeTypeSerializationStrategy(IEnumerable<Type> messageTypes)
        {
            foreach (var type in messageTypes)
            {
                var attribute = type.FindAttribute<MessageTypeNameAttribute>();
                var typeName = attribute == null ? type.Name : attribute.TypeName;
                if (_dregistry.ContainsKey(typeName))
                {
                    string s = string.Format("Unable to register message type name '{0}' for type '{1}'. It is already registered for type '{2}'. Message type name MUST be unique.", typeName, type.Name, _dregistry[typeName].Name);
                    throw new InvalidOperationException(s);
                }
                _sregistry.Add(type, typeName);
                _dregistry.Add(typeName, type);
            }
        }

        public string Serialize(Type type)
        {
            string result;
            return _sregistry.TryGetValue(type, out result) ? result : _fallback.Serialize(type);
        }

        public Type Deserialize(string typeName)
        {
            Type result;
            return _dregistry.TryGetValue(typeName, out result) ? result : _fallback.Deserialize(typeName);
        }
    }
}
