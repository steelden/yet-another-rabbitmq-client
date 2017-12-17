using System;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NLog;

namespace YARC.Utility.Serialization
{
    public class JsonDataSerializationStrategy : IDataSerializationStrategy
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings() { Formatting = Formatting.Indented, ContractResolver = new CamelCasePropertyNamesContractResolver() };
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly ITypeSerializationStrategy _typeSerializer;

        public JsonDataSerializationStrategy(ITypeSerializationStrategy typeSerializer)
        {
            _typeSerializer = typeSerializer;
        }

        public string ConvertFromBytes(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException("bytes");
            try
            {
                return Encoding.UTF8.GetString(bytes);
            }
            catch (Exception ex)
            {
                _logger.Error("Data conversion failed:", ex);
            }
            return null;
        }

        public byte[] ConvertToBytes(string text)
        {
            if (text == null) throw new ArgumentNullException("text");
            try
            {
                return Encoding.UTF8.GetBytes(text);
            }
            catch (Exception ex)
            {
                _logger.Error("Data conversion failed:", ex);
            }
            return null;
        }

        public Tuple<string, byte[]> Serialize(object message)
        {
            if (message == null) throw new ArgumentNullException("message");
            string typeName = _typeSerializer.Serialize(message.GetType());
            string stringValue = JsonConvert.SerializeObject(message);
            var bytesValue = ConvertToBytes(stringValue);
            return new Tuple<string, byte[]>(typeName, bytesValue);
        }

        public Tuple<Type, object> Deserialize(string typeName, byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException("bytes");
            Type type = _typeSerializer.Deserialize(typeName);
            if (type != null)
            {
                try
                {
                    var stringValue = ConvertFromBytes(bytes);
                    var resultValue = JsonConvert.DeserializeObject(stringValue, type);
                    return new Tuple<Type, object>(type, resultValue);
                }
                catch (Exception ex)
                {
                    _logger.Error("Data deserialization failed:", ex);
                }
            }
            return null;
        }

        public T Deserialize<T>(byte[] bytes)
        {
            try
            {
                var stringValue = ConvertFromBytes(bytes);
                var resultValue = JsonConvert.DeserializeObject<T>(stringValue);
                return resultValue;
            }
            catch (Exception ex)
            {
                _logger.Error("Data deserialization failed:", ex);
            }
            return default(T);
        }

        public Tuple<string, string> SerializeString(object message)
        {
            if (message == null) throw new ArgumentNullException("message");
            string typeName = _typeSerializer.Serialize(message.GetType());
            string stringValue = JsonConvert.SerializeObject(message);
            return new Tuple<string, string>(typeName, stringValue);
        }

        public Tuple<Type, object> DeserializeString(string typeName, string text)
        {
            if (text == null) throw new ArgumentNullException("text");
            Type type = _typeSerializer.Deserialize(typeName);
            if (type != null)
            {
                try
                {
                    var resultValue = JsonConvert.DeserializeObject(text, type);
                    return new Tuple<Type, object>(type, resultValue);
                }
                catch (Exception ex)
                {
                    _logger.Error("Data deserialization failed:", ex);
                }
            }
            return null;
        }

        public T DeserializeString<T>(string text)
        {
            try
            {
                var resultValue = JsonConvert.DeserializeObject<T>(text);
                return resultValue;
            }
            catch (Exception ex)
            {
                _logger.Error("Data deserialization failed:", ex);
            }
            return default(T);
        }
    }
}
