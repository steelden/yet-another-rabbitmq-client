using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;

namespace YARC.ExtApi
{
    public class ExtApiConfiguration : IExtApiConfiguration
    {
        private readonly ConcurrentDictionary<string, Func<string, string, Task<IExtApiDataGenerator>>> _registry =
            new ConcurrentDictionary<string, Func<string, string, Task<IExtApiDataGenerator>>>(StringComparer.OrdinalIgnoreCase);

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private string GetKey(string providerName, string objectName, string action)
        {
            if (String.IsNullOrEmpty(providerName)) throw new ArgumentNullException("providerName");
            if (String.IsNullOrEmpty(objectName)) throw new ArgumentNullException("objectName");
            if (String.IsNullOrEmpty(action)) throw new ArgumentNullException("action");
            return string.Format("{0}.{1}.{2}", providerName, objectName, action);
        }

        private T DeserializeParams<T>(string @params)
        {
            T result = default(T);
            if (typeof(T) == typeof(string))
            {
                result = (T)Convert.ChangeType(@params, typeof(T));
            }
            else
            {
                try
                {
                    result = JsonConvert.DeserializeObject<T>(@params);
                }
                catch (Exception ex)
                {
                    _logger.Error("Deserialization failed:", ex);
                }
            }
            return result;
        }

        private void AddProvider(string providerName, string objectName, string action, Func<string, string, Task<IExtApiDataGenerator>> wrapper)
        {
            string key = GetKey(providerName, objectName, action);
            if (!_registry.TryAdd(key, wrapper))
            {
                string s = string.Format("Provider with the same name ({0}), object name ({1}) and action ({2}) is already registered.", providerName, objectName, action);
                throw new InvalidOperationException(s);
            }
        }

        public void RegisterProvider(string providerName, string objectName, string action, IExtApiDataProvider provider)
        {
            RegisterProvider(providerName, objectName, action, provider.HandleRequestAsync);
        }

        public void RegisterProvider(string providerName, string objectName, string action, Func<string, string, string, string, Task<IExtApiDataGenerator>> provider)
        {
            if (provider == null) throw new ArgumentNullException("provider");
            AddProvider(providerName, objectName, action,
                (id, @params) =>
                {
                    return provider(providerName, objectName, action, id);
                });
        }

        public void RegisterProvider<T>(string providerName, string objectName, string action, IExtApiDataProvider<T> provider)
        {
            RegisterProvider<T>(providerName, objectName, action, provider.HandleRequestAsync);
        }

        public void RegisterProvider<T>(string providerName, string objectName, string action, Func<string, string, string, string, T, Task<IExtApiDataGenerator>> provider)
        {
            if (provider == null) throw new ArgumentNullException("provider");
            AddProvider(providerName, objectName, action,
                (id, @params) =>
                {
                    var deserializedParams = DeserializeParams<T>(@params);
                    return provider(providerName, objectName, action, id, deserializedParams);
                });
        }

        public Func<string, string, Task<IExtApiDataGenerator>> FindProvider(string providerName, string objectName, string action)
        {
            Func<string, string, Task<IExtApiDataGenerator>> result;
            return _registry.TryGetValue(GetKey(providerName, objectName, action), out result) ? result : null;
        }

        public bool ContainsProvider(string providerName, string objectName, string action)
        {
            return _registry.ContainsKey(GetKey(providerName, objectName, action));
        }
    }
}
