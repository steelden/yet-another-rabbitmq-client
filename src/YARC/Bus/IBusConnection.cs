using System;
using System.Threading.Tasks;

namespace YARC.Bus
{
    public interface IBusConnection
    {
        IBusConfiguration Configuration { get; }

        void Connect();
        void Close();

        IDisposable ReceiveEvent(string eventName, Action<IHandlerRegistry> registrar);
        IDisposable ReceiveCommand(string commandName, Action<IHandlerRegistry> registrar);
        IDisposable ReceiveRpcRequest(string requestName, Action<IHandlerRegistry> registrar);

        Task SendEvent<T>(string eventName, T message);
        Task SendCommand<T>(string commandName, T message);

        Task SendRpcRequest<T>(string requestName, T message, Action<IHandlerRegistry> registrar);
        Task SendRpcRequest<TRequest, TResponse>(string requestName, TRequest message, Func<TResponse, Task> handler);
        Task<TResponse> SendRpcRequest<TRequest, TResponse>(string requestName, TRequest message);
    }
}
