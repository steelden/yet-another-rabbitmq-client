using System;
using System.Threading.Tasks;

namespace YARC.Bus.Handlers
{
    public interface IRpcHandler<T>
    {
        Task HandleAsync(T message, Action<object> replyAction);
    }
}
