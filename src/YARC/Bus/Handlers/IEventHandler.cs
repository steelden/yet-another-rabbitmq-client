using System.Threading.Tasks;

namespace YARC.Bus.Handlers
{
    public interface IEventHandler<T>
    {
        Task HandleAsync(T message);
    }
}
