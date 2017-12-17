using YARC.Messages.Bus;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YARC.Bus.Handlers;

namespace YARC.Bus
{
    public static class HandlerRegistryExtensions
    {
        public static IHandlerRegistry On<T>(this IHandlerRegistry registry, IEventHandler<T> handler) where T : IEventMessage
        {
            if (handler == null) throw new ArgumentNullException("handler");
            return registry.On<T>(handler.HandleAsync);
        }

        public static IHandlerRegistry On<T>(this IHandlerRegistry registry, ICommandHandler<T> handler) where T : ICommandMessage
        {
            if (handler == null) throw new ArgumentNullException("handler");
            return registry.On<T>(handler.HandleAsync);
        }

        public static IHandlerRegistry On<T>(this IHandlerRegistry registry, IRpcHandler<T> handler) where T : IRpcRequestMessage
        {
            if (handler == null) throw new ArgumentNullException("handler");
            return registry.On<T>(handler.HandleAsync);
        }

        public static IHandlerRegistry On<T>(this IHandlerRegistry registry, Func<T, Task> handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            return registry.On(typeof(T), (x, a) => handler((T)x));
        }

        public static IHandlerRegistry On<T>(this IHandlerRegistry registry, Func<T, Action<object>, Task> handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            return registry.On(typeof(T), (x, a) => handler((T)x, a));
        }

        public static IHandlerRegistry On(this IHandlerRegistry registry, Type type, Func<object, Task> handler)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (handler == null) throw new ArgumentNullException("handler");
            return registry.On(type, (x, a) => handler(x));
        }

        public static IEnumerable<Func<object, Action<object>, Task>> FindHandler<T>(this IHandlerRegistry registry)
        {
            return registry.FindHandler(typeof(T));
        }
    }
}
