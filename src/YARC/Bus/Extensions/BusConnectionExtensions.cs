using YARC.Messages.Attributes;
using System;
using System.Threading.Tasks;
using YARC.Bus.Handlers;

namespace YARC.Bus
{
    public static class BusConnectionExtensions
    {
        private static string GetMessageName(Type type)
        {
            var attribute = type.FindAttribute<MessageNameAttribute>();
            if (attribute == null)
            {
                string s = string.Format("MessageName attribute not found for class '{0}'.", type.Name);
                throw new InvalidOperationException(s);
            }
            return attribute.Name;
        }

        public static IDisposable ReceiveEvent<T>(this IBusConnection connection, string eventName, Func<T, Task> handler)
        {
            return connection.ReceiveEvent(eventName, x => x.On(handler));
        }

        public static IDisposable ReceiveCommand<T>(this IBusConnection connection, string commandName, Func<T, Task> handler)
        {
            return connection.ReceiveCommand(commandName, x => x.On(handler));
        }

        public static IDisposable ReceiveRpcRequest<T>(this IBusConnection connection, string requestName, Func<T, Action<object>, Task> handler)
        {
            return connection.ReceiveRpcRequest(requestName, x => x.On(handler));
        }

        public static IDisposable ReceiveEvent<T>(this IBusConnection connection, string eventName, IEventHandler<T> handler)
        {
            return connection.ReceiveEvent<T>(eventName, handler.HandleAsync);
        }

        public static IDisposable ReceiveCommand<T>(this IBusConnection connection, string commandName, ICommandHandler<T> handler)
        {
            return connection.ReceiveCommand<T>(commandName, handler.HandleAsync);
        }

        public static IDisposable ReceiveRpcRequest<T>(this IBusConnection connection, string requestName, IRpcHandler<T> handler)
        {
            return connection.ReceiveRpcRequest<T>(requestName, handler.HandleAsync);
        }


        public static IDisposable ReceiveEvent<T>(this IBusConnection connection, Func<T, Task> handler)
        {
            return connection.ReceiveEvent(GetMessageName(typeof(T)), x => x.On(handler));
        }

        public static IDisposable ReceiveCommand<T>(this IBusConnection connection, Func<T, Task> handler)
        {
            return connection.ReceiveCommand(GetMessageName(typeof(T)), x => x.On(handler));
        }

        public static IDisposable ReceiveRpcRequest<T>(this IBusConnection connection, Func<T, Action<object>, Task> handler)
        {
            return connection.ReceiveRpcRequest(GetMessageName(typeof(T)), x => x.On(handler));
        }

        public static IDisposable ReceiveEvent<T>(this IBusConnection connection, IEventHandler<T> handler)
        {
            return connection.ReceiveEvent<T>(handler.HandleAsync);
        }

        public static IDisposable ReceiveCommand<T>(this IBusConnection connection, ICommandHandler<T> handler)
        {
            return connection.ReceiveCommand<T>(handler.HandleAsync);
        }

        public static IDisposable ReceiveRpcRequest<T>(this IBusConnection connection, IRpcHandler<T> handler)
        {
            return connection.ReceiveRpcRequest<T>(handler.HandleAsync);
        }
    }
}
