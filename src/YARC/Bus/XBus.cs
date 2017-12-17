using System;

namespace YARC.Bus
{
    public static class XBus
    {
        public static IBusConnection Instance { get; private set; }

        public static IBusConnection Initialize(Action<IBusConfiguration> configAction)
        {
            var config = new BusConfiguration();
            configAction(config);
            config.Build();
            return Instance = new EasyConnection(config);
        }

        private static void ThrowIfUninitialized()
        {
            if (Instance == null) throw new InvalidOperationException("Bus was not initialized.");
        }

        public static void Connect()
        {
            ThrowIfUninitialized();
            Instance.Connect();
        }

        public static void Close()
        {
            ThrowIfUninitialized();
            Instance.Close();
        }
    }
}
