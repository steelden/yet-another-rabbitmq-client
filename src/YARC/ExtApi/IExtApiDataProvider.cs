using System.Threading.Tasks;

namespace YARC.ExtApi
{
    public interface IExtApiDataProvider<T>
    {
        Task<IExtApiDataGenerator> HandleRequestAsync(string providerName, string objectName, string action, string id, T @params);
    }

    public interface IExtApiDataProvider
    {
        Task<IExtApiDataGenerator> HandleRequestAsync(string providerName, string objectName, string action, string id);
    }
}
