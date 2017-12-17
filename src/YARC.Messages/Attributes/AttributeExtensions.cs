using System;
using System.Linq;

namespace YARC.Messages.Attributes
{
    public static class AttributeExtensions
    {
        public static T FindAttribute<T>(this Type type) where T : Attribute
        {
            return (T)type
                .GetCustomAttributes(typeof(T), false)
                .FirstOrDefault();
        }
    }
}
