using System;
using System.Threading.Tasks;

namespace YARC.ExtApi
{
    public interface IExtApiConfiguration
    {
        void RegisterProvider(string providerName, string objectName, string action, IExtApiDataProvider provider);
        void RegisterProvider(string providerName, string objectName, string action, Func<string, string, string, string, Task<IExtApiDataGenerator>> provider);
        void RegisterProvider<T>(string providerName, string objectName, string action, IExtApiDataProvider<T> provider);
        void RegisterProvider<T>(string providerName, string objectName, string action, Func<string, string, string, string, T, Task<IExtApiDataGenerator>> provider);
        Func<string, string, Task<IExtApiDataGenerator>> FindProvider(string providerName, string objectName, string action);
        bool ContainsProvider(string providerName, string objectName, string action);
    }
}
