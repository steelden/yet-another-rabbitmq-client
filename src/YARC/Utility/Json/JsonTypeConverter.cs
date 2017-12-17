using System;
using Newtonsoft.Json;

namespace YARC.Utility.Json
{
    public class JsonTypeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            if (objectType == typeof(Type)) return true;
            return false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var typeName = (string)reader.Value;
            var parts = typeName.Split(':');
            if (parts.Length == 2)
            {
                string fullName = string.Format("{0}, {1}", parts[0], parts[1]);
                return Type.GetType(fullName, false);
            }
            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var type = (Type)value;
            var newValue = type.FullName + ":" + type.Assembly.GetName().Name;
            serializer.Serialize(writer, newValue);
        }
    }
}
