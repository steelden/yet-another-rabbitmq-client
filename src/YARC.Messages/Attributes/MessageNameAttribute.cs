using System;

namespace YARC.Messages.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class MessageNameAttribute : Attribute
    {
        public string Name { get; private set; }

        public MessageNameAttribute(string name)
        {
            Name = name;
        }
    }
}
