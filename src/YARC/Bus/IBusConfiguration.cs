using System;
using System.Collections.Generic;
using YARC.ExtApi;
using YARC.Messages.Bus;
using YARC.Utility.Serialization;

namespace YARC.Bus
{
    public interface IBusConfiguration
    {
        string BusConnectionString { get; }
        IBusTimeouts Timeouts { get; }
        IDataSerializationStrategy DataSerializationStrategy { get; }
        ITypeSerializationStrategy TypeSerializationStrategy { get; }
        IEnumerable<Action<IBusConnection>> Configurators { get; }

        IBusConfiguration SetBusConnectionString(string connectionString);
        IBusConfiguration LoadBusConnectionString(string configName);
        IBusConfiguration SetDataSerializationStrategy(IDataSerializationStrategy strategy);
        IBusConfiguration SetTypeSerializationStrategy(ITypeSerializationStrategy strategy);
        IBusConfiguration SetTimeouts(IBusTimeouts timeouts);

        IBusConfiguration AddConfigurator(Action<IBusConnection> configurator);
        IBusConfiguration AddModule(IIntegrationModule module);
        IBusConfiguration AddModules(IEnumerable<IIntegrationModule> modules);

        IBusConfiguration AddExtApiModule(IExtApiModule module);
        IBusConfiguration AddExtApiModules(IEnumerable<IExtApiModule> modules);

        IBusConfiguration AddMessageType(Type messageType);
        IBusConfiguration AddMessageType<T>() where T : IMessage;
        IBusConfiguration AddMessageTypes(IEnumerable<Type> messageTypes);
        IBusConfiguration AddMessageTypes(IEnumerable<IMessage> messages);
    }
}
