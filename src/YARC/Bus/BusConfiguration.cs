using System;
using System.Collections.Generic;
using System.Configuration;
using YARC.ExtApi;
using YARC.Messages.Bus;
using YARC.Messages.ExtApi;
using YARC.Utility.Serialization;

namespace YARC.Bus
{
    public sealed class BusConfiguration : IBusConfiguration
    {
        public string BusConnectionString { get; private set; }
        public string IntegrationDbConnectionString { get; private set; }

        private IBusTimeouts _timeouts = null;
        private readonly IList<Action<IBusConnection>> _configurators = new List<Action<IBusConnection>>();
        private readonly IList<Action<IExtApiConfiguration>> _extApiConfigurators = new List<Action<IExtApiConfiguration>>();
        private readonly IList<Type> _messageTypes = new List<Type>();

        private ITypeSerializationStrategy _typeStrategy = null;
        private IDataSerializationStrategy _dataStrategy = null;
        private bool _finished = false;

        public IBusTimeouts Timeouts { get { return _timeouts; } }

        public BusConfiguration()
        {
        }

        public IDataSerializationStrategy DataSerializationStrategy { get { return _dataStrategy; } }
        public ITypeSerializationStrategy TypeSerializationStrategy { get { return _typeStrategy; } }
        public IEnumerable<Action<IBusConnection>> Configurators { get { return _configurators; } }
        public IEnumerable<Action<IExtApiConfiguration>> ExtApiConfigurators { get { return _extApiConfigurators; } }

        private void ThrowIfFinished()
        {
            if (_finished) throw new InvalidOperationException("Configuration already built.");
        }

        public IBusConfiguration SetBusConnectionString(string connectionString)
        {
            ThrowIfFinished();
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException("connectionString");
            BusConnectionString = connectionString;
            return this;
        }

        public IBusConfiguration LoadBusConnectionString(string configName)
        {
            ThrowIfFinished();
            if (string.IsNullOrEmpty(configName)) throw new ArgumentNullException("configName");
            var cs = ConfigurationManager.ConnectionStrings[configName];
            if (cs == null) throw new InvalidOperationException("Unable to find connection string in configuration file.");
            SetBusConnectionString(cs.ConnectionString);
            return this;
        }

        public IBusConfiguration SetTypeSerializationStrategy(ITypeSerializationStrategy strategy)
        {
            ThrowIfFinished();
            if (strategy == null) throw new ArgumentNullException("strategy");
            _typeStrategy = strategy;
            return this;
        }

        public IBusConfiguration SetDataSerializationStrategy(IDataSerializationStrategy strategy)
        {
            ThrowIfFinished();
            if (strategy == null) throw new ArgumentNullException("strategy");
            _dataStrategy = strategy;
            return this;
        }

        public IBusConfiguration SetTimeouts(IBusTimeouts timeouts)
        {
            ThrowIfFinished();
            if (timeouts == null) throw new ArgumentNullException("timeouts");
            _timeouts = timeouts;
            return this;
        }

        public IBusConfiguration AddConfigurator(Action<IBusConnection> configurator)
        {
            ThrowIfFinished();
            if (configurator == null) throw new ArgumentNullException("configurator");
            _configurators.Add(configurator);
            return this;
        }

        public IBusConfiguration AddModule(IIntegrationModule module)
        {
            ThrowIfFinished();
            if (module == null) throw new ArgumentNullException("module");
            return AddConfigurator(x => module.RegisterEndpoints(x));
        }

        public IBusConfiguration AddModules(IEnumerable<IIntegrationModule> modules)
        {
            ThrowIfFinished();
            if (modules == null) throw new ArgumentNullException("modules");
            foreach (var module in modules)
            {
                AddModule(module);
            }
            return this;
        }

        public IBusConfiguration AddExtApiModule(IExtApiModule module)
        {
            ThrowIfFinished();
            if (module == null) throw new ArgumentNullException("module");
            _extApiConfigurators.Add(x => module.RegisterProviders(x));
            return this;
        }

        public IBusConfiguration AddExtApiModules(IEnumerable<IExtApiModule> modules)
        {
            ThrowIfFinished();
            if (modules == null) throw new ArgumentNullException("modules");
            foreach (var module in modules)
            {
                AddExtApiModule(module);
            }
            return this;
        }

        public IBusConfiguration AddMessageType<T>() where T : IMessage
        {
            return AddMessageType(typeof(T));
        }

        public IBusConfiguration AddMessageType(Type messageType)
        {
            ThrowIfFinished();
            if (messageType == null) throw new ArgumentNullException("messageType");
            if (!typeof(IMessage).IsAssignableFrom(messageType)) throw new ArgumentException("Message type must be a derivative of IMessage.");
            _messageTypes.Add(messageType);
            return this;
        }

        public IBusConfiguration AddMessageTypes(IEnumerable<Type> messageTypes)
        {
            if (messageTypes == null) throw new ArgumentNullException("messageTypes");
            foreach (var messageType in messageTypes)
            {
                AddMessageType(messageType);
            }
            return this;
        }

        public IBusConfiguration AddMessageTypes(IEnumerable<IMessage> messages)
        {
            if (messages == null) throw new ArgumentNullException("messages");
            foreach (var message in messages)
            {
                AddMessageType(message.GetType());
            }
            return this;
        }

        public void Build()
        {
            if (_timeouts == null) _timeouts = new BusTimeouts();
            if (_typeStrategy == null) _typeStrategy = new AttributeTypeSerializationStrategy(_messageTypes);
            if (_dataStrategy == null) _dataStrategy = new JsonDataSerializationStrategy(_typeStrategy);
            if (_extApiConfigurators.Count > 0)
            {
                var extConfig = new ExtApiConfiguration();
                foreach (var configurator in _extApiConfigurators)
                {
                    configurator(extConfig);
                }
                var imodule = new ExtApiIntegrationModule(extConfig);
                AddModule(imodule);
                AddMessageType<ExtApiDataRequest>();
                AddMessageType<ExtApiDataResponse>();
                AddMessageType<ExtApiStatusResponse>();
            }
            _finished = true;
        }
    }
}
