using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YARC.Bus
{
    public interface IHandlerRegistry
    {
        IHandlerRegistry On(Type type, Func<object, Action<object>, Task> handler);
        IHandlerRegistry OnError(Type type, Func<string, Action<object>, Task<bool>> onError);
        IHandlerRegistry OnTimeout(Func<Task<bool>> onTimeout);

        IEnumerable<Func<object, Action<object>, Task>> FindHandler(Type type);
        Func<Task<bool>> FindTimeoutHandler();
        Func<string, Action<object>, Task<bool>> FindErrorHandler(Type type);

        //IDictionary<Type, IList<Func<object, Action<object>, Task>>> GetRegisteredHandlers();
    }
}
