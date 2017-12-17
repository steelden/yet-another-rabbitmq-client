using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace YARC.Utility
{
    public static class Invoker
    {
        public static void InvokeWhen<T>(T instance, object command)
        {
            InternalInvokeWhen<T>(instance, command, true);
        }

        public static void InvokeWhenOptional<T>(T instance, object command)
        {
            InternalInvokeWhen<T>(instance, command, false);
        }

        private static readonly MethodInfo _internalPreserveStackTraceMethod =
            typeof(Exception).GetMethod("InternalPreserveStackTrace", BindingFlags.Instance | BindingFlags.NonPublic);

        private static class Cache<T>
        {
            public static readonly IDictionary<Type, MethodInfo> Info = typeof(T)
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(x => x.Name == "When")
                .Where(x => x.GetParameters().Length == 1)
                .ToDictionary(x => x.GetParameters().First().ParameterType, x => x);
        }

        private static void InternalInvokeWhen<T>(T instance, object command, bool throwIfNotFound)
        {
            MethodInfo info;
            var type = command.GetType();
            if (!Cache<T>.Info.TryGetValue(type, out info))
            {
                if (throwIfNotFound)
                {
                    string msg = string.Format("Failed to locate {0}.When({1})", typeof(T).Name, type.Name);
                    throw new InvalidOperationException(msg);
                }
                return;
            }
            try
            {
                info.Invoke(instance, new[] { command });
            }
            catch (TargetInvocationException ex)
            {
                if (_internalPreserveStackTraceMethod != null) _internalPreserveStackTraceMethod.Invoke(ex.InnerException, new object[0]);
                throw ex.InnerException;
            }
        }
    }
}
