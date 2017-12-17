using System.Threading.Tasks;

namespace YARC.Bus.Handlers
{
    public interface ICommandHandler<T>
    {
        Task HandleAsync(T message);
    }
}
