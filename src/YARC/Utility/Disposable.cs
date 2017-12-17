using System;

namespace YARC.Utility
{
    public class Disposable : IDisposable
    {
        private readonly Action _disposeAction;

        private Disposable(Action disposeAction)
        {
            if (disposeAction == null) throw new ArgumentNullException("disposeAction");
            _disposeAction = disposeAction;
        }

        public void Dispose()
        {
            _disposeAction();
        }

        public static IDisposable Create(Action disposeAction)
        {
            return new Disposable(disposeAction);
        }

        public static IDisposable Create(params IDisposable[] objects)
        {
            return Create(() =>
            {
                foreach (var obj in objects)
                {
                    obj.Dispose();
                }
            });
        }
    }
}
