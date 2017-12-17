using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YARC.Bus
{
    public class HandlerRegistry : IHandlerRegistry
    {
        private readonly ConcurrentDictionary<Type, IList<Func<object, Action<object>, Task>>> _registry =
            new ConcurrentDictionary<Type, IList<Func<object, Action<object>, Task>>>();
        private readonly ConcurrentDictionary<Type, Func<string, Action<object>, Task<bool>>> _errorRegistry =
            new ConcurrentDictionary<Type, Func<string, Action<object>, Task<bool>>>();

        private Func<Task<bool>> _onTimeout;

        public IHandlerRegistry On(Type type, Func<object, Action<object>, Task> handler)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (handler == null) throw new ArgumentNullException("handler");
            _registry.AddOrUpdate(type,
                t => new List<Func<object, Action<object>, Task>>() { handler },
                (t, oldvalue) => { oldvalue.Add(handler); return oldvalue; });
            return this;
        }

        public IEnumerable<Func<object, Action<object>, Task>> FindHandler(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");
            IList<Func<object, Action<object>, Task>> result = null;
            if (!_registry.TryGetValue(type, out result))
            {
                return _registry
                    .Where(x => x.Key.IsAssignableFrom(type))
                    .Select(x => x.Value)
                    .FirstOrDefault();
            }
            return result;
        }

        public IDictionary<Type, IList<Func<object, Action<object>, Task>>> GetRegisteredHandlers()
        {
            return _registry;
        }

        public IHandlerRegistry OnTimeout(Func<Task<bool>> onTimeout)
        {
            _onTimeout = onTimeout;
            return this;
        }

        public Func<Task<bool>> FindTimeoutHandler()
        {
            return _onTimeout;
        }

        public IHandlerRegistry OnError(Type type, Func<string, Action<object>, Task<bool>> onError)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (onError == null) throw new ArgumentNullException("onError");
            _errorRegistry[type] = onError;
            return this;
        }

        public Func<string, Action<object>, Task<bool>> FindErrorHandler(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");
            Func<string, Action<object>, Task<bool>> result = null;
            if (!_errorRegistry.TryGetValue(type, out result))
            {
                return _errorRegistry
                    .Where(x => x.Key.IsAssignableFrom(type))
                    .Select(x => x.Value)
                    .FirstOrDefault();
            }
            return result;
        }
    }
}
