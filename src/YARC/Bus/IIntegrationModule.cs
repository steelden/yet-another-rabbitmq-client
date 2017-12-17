
namespace YARC.Bus
{
    public interface IIntegrationModule
    {
        void RegisterEndpoints(IBusConnection connection);
    }
}
