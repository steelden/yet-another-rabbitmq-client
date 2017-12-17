using System;

namespace YARC.Messages.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class MessageTypeNameAttribute : Attribute
    {
        public string TypeName { get; private set; }

        public MessageTypeNameAttribute(string typeName)
        {
            TypeName = typeName;
        }
    }
}
