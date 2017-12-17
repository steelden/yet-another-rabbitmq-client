using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ;
using EasyNetQ.Topology;
using NLog;
using YARC.Utility;
using YARC.Utility.Serialization;

namespace YARC.Bus
{
    public class EasyConnection : IBusConnection, IDisposable
    {
        private readonly IDataSerializationStrategy _serializationStrategy;
        private readonly IBusConfiguration _config;

        private IBus _bus;
        private readonly ConcurrentDictionary<string, IDisposable> _subscribers = new ConcurrentDictionary<string, IDisposable>();
        private readonly ConcurrentDictionary<string, IExchange> _exchanges = new ConcurrentDictionary<string, IExchange>();
        private readonly ConcurrentDictionary<string, IQueue> _queues = new ConcurrentDictionary<string, IQueue>();
        private readonly ConcurrentDictionary<string, IBinding> _bindings = new ConcurrentDictionary<string, IBinding>();
        private readonly ConcurrentDictionary<string, Func<Tuple<Type, object>, Task>> _rpcHandlers = new ConcurrentDictionary<string, Func<Tuple<Type, object>, Task>>();
        private readonly ConcurrentDictionary<string, IHandlerRegistry> _handlers = new ConcurrentDictionary<string, IHandlerRegistry>();
        private Guid _clientId;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public IBusConfiguration Configuration { get { return _config; } }

        public EasyConnection(IBusConfiguration config)
        {
            _config = config;
            _serializationStrategy = config.DataSerializationStrategy;

            _clientId = Guid.NewGuid();
        }

        private void ThrowIfBusIsUninitialized()
        {
            if (_bus == null) throw new InvalidOperationException("Bus is not initialized. Call Connect() first.");
        }

        private IBus CreateBus()
        {
            string connString = _config.BusConnectionString;
            if (!connString.Contains("timeout="))
            {
                connString = string.Format("{0};timeout={1}", connString, _config.Timeouts.RpcRequestTimeout * 1000);
            }

            Action<IServiceRegister> registerServices = x => x
                .Register<IEasyNetQLogger, NLogEasyLogger>()
                .Register<IConventions, EasyConventions>();
            return RabbitHutch.CreateBus(connString, registerServices);
        }

        public void Connect()
        {
            _bus = CreateBus();
            CreateEventsExchange();
            CreateCommandsExchange();
            CreateRpcRequestExchange();
            StartRpcResponseConsumer();
            foreach (var configurator in _config.Configurators)
            {
                configurator(this);
            }
        }

        public void Close()
        {
            ThrowIfBusIsUninitialized();
            RemoveAll();
            _bus.Dispose();
            _bus = null;
        }

        private IExchange CreateExchange(string exchangeName, string type = ExchangeType.Topic, bool passive = false, bool durable = true, bool autoDelete = false)
        {
            return _exchanges.GetOrAdd(exchangeName,
                name =>
                {
                    return InvokeSafe(x => x.ExchangeDeclare(name, type, passive, durable, autoDelete));
                });
        }

        private IQueue CreateQueue(string queueName, int perQueueTtl = int.MaxValue, int expires = int.MaxValue, bool exclusive = false, bool passive = false)
        {
            return _queues.GetOrAdd(queueName,
                name =>
                {
                    return InvokeSafe(x => x.QueueDeclare(name, passive, !exclusive, exclusive, false, perQueueTtl, expires));
                });
        }

        private IExchange CreateEventsExchange()
        {
            return CreateExchange(BusNames.EventsExchangeName);
        }

        private IExchange CreateCommandsExchange()
        {
            return CreateExchange(BusNames.CommandsExchangeName);
        }

        private IExchange CreateRpcRequestExchange()
        {
            return CreateExchange(BusNames.RpcRequestExchangeName);
        }

        private IExchange CreateRpcResponseExchange()
        {
            return CreateExchange(BusNames.RpcResponseExchangeName);
        }

        private IQueue CreateEventQueue(string eventName, Guid subscriberId)
        {
            string queueName = string.Format("{0}.{1}", eventName, subscriberId);
            return CreateQueue(queueName, expires: _config.Timeouts.EventQueueTimeout * 1000);
        }

        private IQueue CreateCommandQueue(string commandName)
        {
            return CreateQueue(commandName);
        }

        private IQueue CreateRpcRequestQueue(string requestName, int messageTtl = int.MaxValue)
        {
            return CreateQueue(requestName, messageTtl);
        }

        private IQueue CreateRpcResponseQueue()
        {
            string queueName = string.Format("{0}.{1}", BusNames.RpcResponseQueue, _clientId);
            return CreateQueue(queueName,
                perQueueTtl: _config.Timeouts.RpcRequestTimeout * 1000,
                expires: _config.Timeouts.RpcQueueTimeout * 1000);
        }

        private IBinding Bind(IExchange exchange, IQueue queue, string routingKey)
        {
            string bindingName = string.Format("{0}_{1}_{2}", exchange.Name, queue.Name, routingKey);
            return _bindings.GetOrAdd(bindingName,
                name =>
                {
                    return InvokeSafe(x => x.Bind(exchange, queue, routingKey));
                });
        }

        private void Unbind(IBinding binding)
        {
            string bindingName = string.Format("{0}_{1}_{2}", binding.Exchange.Name, ((IQueue)binding.Bindable).Name, binding.RoutingKey);
            if (_bindings.TryRemove(bindingName, out binding))
            {
                InvokeSafe(x => x.BindingDelete(binding));
            }
        }

        private void RemoveSubscriber(string name)
        {
            IDisposable sub;
            if (_subscribers.TryRemove(name, out sub))
            {
                sub.Dispose();
            }
        }

        private void RemoveQueue(IQueue queue)
        {
            if (_queues.TryRemove(queue.Name, out queue))
            {
                InvokeSafe(x => x.QueueDelete(queue, true, true));
            }
        }

        private void RemoveExchange(IExchange exchange)
        {
            if (_exchanges.TryRemove(exchange.Name, out exchange))
            {
                InvokeSafe(x => x.ExchangeDelete(exchange, true));
            }
        }

        private async Task<bool> CallHandlers(IHandlerRegistry handlers, Type type, object message, Action<object> action)
        {
            var result = true;
            var errorHandler = handlers.FindErrorHandler(type);
            var typeHandlers = handlers.FindHandler(type);
            if (typeHandlers != null)
            {
                foreach (var handler in typeHandlers)
                {
                    var hresult = true;
                    var errorMessage = "No error.";
                    try
                    {
                        await handler(message, action);
                    }
                    catch (Exception ex)
                    {
                        errorMessage = string.Format("Message handler failed: {0}", ex.Message);
                        _logger.Error("Message handler exception:", ex);
                        hresult = false;
                    }
                    if (!hresult && errorHandler != null)
                    {
                        try
                        {
                            await errorHandler(errorMessage, action);
                        }
                        catch (Exception ex)
                        {
                            _logger.Error("Error handler exception:", ex);
                        }
                    }
                    result = result && hresult;
                }
            }
            else
            {
                _logger.Error("Handler for type '{0}' not found. Dropping message.", type.Name);
                result = false;
            }
            return result;
        }

        private async Task ReceivedEventAndCommandHandler(byte[] bytes, MessageProperties props, MessageReceivedInfo info, IHandlerRegistry handlers)
        {
            string prefix = string.Format("[{0}/{1}]", info.Exchange, info.Queue);
            if (props.TypePresent)
            {
                var deserialized = _serializationStrategy.Deserialize(props.Type, bytes);
                if (deserialized != null)
                {
                    await CallHandlers(handlers, deserialized.Item1, deserialized.Item2, null);
                }
                else
                {
                    _logger.Error("{0} Message deserialization failed. Dropping message.", prefix);
                }
            }
            else
            {
                _logger.Warn("{0} Message does not contain Type header. Dropping message.", prefix);
            }
        }

        private async Task<bool> ReceivedRpcResponseHandler(Tuple<Type, object> deserialized, IHandlerRegistry handlers, string exchangeName, string queueName)
        {
            bool finished = true;
            string prefix = string.Format("[{0}/{1}]", exchangeName, queueName);
            if (deserialized != null)
            {
                if (!await CallHandlers(handlers, deserialized.Item1, deserialized.Item2, _ => finished = false))
                {
                    finished = true;
                }
            }
            else
            {
                _logger.Error("{0} RPC response message deserialization failed.", prefix);
                finished = true;
            }
            return finished;
        }

        private IHandlerRegistry GetHandlers(string name, Action<IHandlerRegistry> registrar)
        {
            var result = _handlers.GetOrAdd(name, _ => new HandlerRegistry());
            if (registrar != null)
            {
                registrar(result);
            }
            return result;
        }

        private IDisposable StartConsumer(string consumerName, IQueue queue, Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage)
        {
            if (_subscribers.ContainsKey(consumerName))
            {
                var msg = string.Format("Consumer '{0}' already exists.", consumerName);
                throw new InvalidOperationException(msg);
            }
            var consumer = _bus.Advanced.Consume(queue, onMessage);
            var unsub = Disposable.Create(() => RemoveSubscriber(consumerName));
            _subscribers.TryAdd(consumerName, unsub);
            return unsub;
        }

        public IDisposable ReceiveEvent(string eventName, Action<IHandlerRegistry> registrar)
        {
            var subscriberId = Guid.NewGuid();
            var exchange = CreateEventsExchange();
            var queue = CreateEventQueue(eventName, subscriberId);
            var binding = Bind(exchange, queue, eventName);
            var handlers = GetHandlers(queue.Name, registrar);
            var consumer = StartConsumer(queue.Name, queue,
                (bytes, props, info) => ReceivedEventAndCommandHandler(bytes, props, info, handlers));
            var disposeAction = Disposable.Create(() => Unbind(binding));
            return Disposable.Create(consumer, disposeAction);
        }

        public IDisposable ReceiveCommand(string commandName, Action<IHandlerRegistry> registrar)
        {
            var handlers = GetHandlers(commandName, registrar);
            var exchange = CreateCommandsExchange();
            var queue = CreateCommandQueue(commandName);
            var binding = Bind(exchange, queue, commandName);
            return StartConsumer(commandName, queue,
                (bytes, props, info) => ReceivedEventAndCommandHandler(bytes, props, info, handlers));
        }

        public IDisposable ReceiveRpcRequest(string requestName, Action<IHandlerRegistry> registrar)
        {
            var handlers = GetHandlers(requestName, registrar);
            var exchange = CreateRpcRequestExchange();
            var queue = CreateRpcRequestQueue(requestName);
            var binding = Bind(exchange, queue, requestName);
            return StartConsumer(requestName, queue,
                async (bytes, props, info) =>
                {
                    if (ValidateMessage(bytes, props, info))
                    {
                        var correlationId = props.CorrelationId;
                        var deserialized = _serializationStrategy.Deserialize(props.Type, bytes);
                        if (deserialized != null)
                        {
                            await CallHandlers(handlers, deserialized.Item1, deserialized.Item2, x => SendRpcResponse(requestName, x, correlationId));
                        }
                    }
                });
        }

        private IDisposable StartRpcResponseConsumer()
        {
            var exchange = CreateRpcResponseExchange();
            var queue = CreateRpcResponseQueue();
            return StartConsumer(queue.Name, queue,
                async (bytes, props, info) =>
                {
                    Func<Tuple<Type, object>, Task> handler;
                    if (props.TypePresent &&
                        props.CorrelationIdPresent &&
                        _rpcHandlers.TryGetValue(props.CorrelationId, out handler))
                    {
                        var deserialized = _serializationStrategy.Deserialize(props.Type, bytes);
                        await handler(deserialized);
                    }
                });
        }

        private void ReceiveRpcResponse(string requestName, string correlationId, string handlerId)
        {
            var exchange = CreateRpcResponseExchange();
            var queue = CreateRpcResponseQueue();
            var binding = Bind(exchange, queue, correlationId);
            var handlers = GetHandlers(handlerId, null);
            Action stopReceiving = () =>
            {
                IHandlerRegistry removedHandlers;
                if (_handlers.TryRemove(handlerId, out removedHandlers))
                {
                    Unbind(binding);
                    Func<Tuple<Type, object>, Task> removedResponseHandlers;
                    _rpcHandlers.TryRemove(correlationId, out removedResponseHandlers);
                }
            };
            var timer = new Timer(
                state =>
                {
                    ((Timer)state).Dispose();
                    var handler = handlers.FindTimeoutHandler();
                    if (handler != null)
                    {
                        handler().ContinueWith(
                            task =>
                            {
                                if (task.IsFaulted || task.Result) stopReceiving();
                            });
                    }
                });
            timer.Change(TimeSpan.FromSeconds(_config.Timeouts.RpcRequestTimeout), Timeout.InfiniteTimeSpan);
            _rpcHandlers.TryAdd(correlationId,
                async deserialized =>
                {
                    timer.Dispose();
                    if (await ReceivedRpcResponseHandler(deserialized, handlers, exchange.Name, queue.Name))
                    {
                        stopReceiving();
                    }
                });
        }

        public async Task SendEvent<T>(string eventName, T message)
        {
            var exchange = CreateEventsExchange();
            var serialized = _serializationStrategy.Serialize(message);
            var props = new MessageProperties() { Type = serialized.Item1, DeliveryMode = 2 };
            await _bus.Advanced.PublishAsync(exchange, eventName, false, false, props, serialized.Item2);
        }

        public async Task SendCommand<T>(string commandName, T message)
        {
            var exchange = CreateCommandsExchange();
            var serialized = _serializationStrategy.Serialize(message);
            var props = new MessageProperties() { Type = serialized.Item1, DeliveryMode = 2 };
            await _bus.Advanced.PublishAsync(exchange, commandName, false, false, props, serialized.Item2);
        }

        public async Task SendRpcRequest<T>(string requestName, T message, Action<IHandlerRegistry> registrar)
        {
            var correlationId = Guid.NewGuid().ToString();
            var handlerId = string.Format("{0}.{1}", requestName, correlationId);
            var handlers = GetHandlers(handlerId, registrar);
            var exchange = CreateRpcRequestExchange();
            var serialized = _serializationStrategy.Serialize(message);
            var props = new MessageProperties()
            {
                Type = serialized.Item1,
                ReplyTo = _clientId.ToString(),
                CorrelationId = correlationId,
                DeliveryMode = 1
            };
            ReceiveRpcResponse(requestName, correlationId, handlerId);
            await _bus.Advanced.PublishAsync(exchange, requestName, false, false, props, serialized.Item2);
        }

        public Task SendRpcRequest<TRequest, TResponse>(string requestName, TRequest message, Func<TResponse, Task> handler)
        {
            return SendRpcRequest(requestName, message, x => x.On<TResponse>(handler));
        }

        public Task<TResponse> SendRpcRequest<TRequest, TResponse>(string requestName, TRequest message)
        {
            var tcs = new TaskCompletionSource<TResponse>();
            var task = SendRpcRequest(requestName, message, x => x.On<TResponse>(
                response =>
                {
                    if (response == null)
                    {
                        tcs.TrySetCanceled();
                    }
                    else
                    {
                        tcs.TrySetResult(response);
                    }
                    return Task.FromResult(0);
                }));
            return tcs.Task;
        }

        private Task SendRpcResponse<T>(string requestName, T message, string correlationId)
        {
            var exchange = CreateRpcResponseExchange();
            var serialized = _serializationStrategy.Serialize(message);
            var props = new MessageProperties() { Type = serialized.Item1, CorrelationId = correlationId, DeliveryMode = 1 };
            return _bus.Advanced.PublishAsync(exchange, correlationId, false, false, props, serialized.Item2);
        }

        private void RemoveAll()
        {
            foreach (var subscription in _subscribers.Values)
            {
                subscription.Dispose();
            }
            foreach (var queue in _queues.Values)
            {
                InvokeSafe(x => x.QueueDelete(queue, true, true));
            }
            foreach (var exchange in _exchanges.Values)
            {
                InvokeSafe(x => x.ExchangeDelete(exchange, true));
            }
        }

        private void InvokeSafe(Action<IAdvancedBus> action)
        {
            try
            {
                action(_bus.Advanced);
            }
            catch (AggregateException ex)
            {
                _logger.Warn("Advanced bus invoke failed: {0}", ex.InnerException.Message);
            }
        }

        private T InvokeSafe<T>(Func<IAdvancedBus, T> fn)
        {
            try
            {
                return fn(_bus.Advanced);
            }
            catch (AggregateException ex)
            {
                _logger.Warn("Advanced bus invoke failed: {0}", ex.InnerException.Message);
            }
            return default(T);
        }

        private bool ValidateMessage(byte[] bytes, MessageProperties props, MessageReceivedInfo info, string expectedCorrelationId = null)
        {
            string errorMessage = "";
            if (!props.TypePresent || string.IsNullOrWhiteSpace(props.Type))
            {
                errorMessage = "Message does not contain Type header.";
            }
            else if (!props.CorrelationIdPresent || string.IsNullOrWhiteSpace(props.CorrelationId))
            {
                errorMessage = "Message does not contain CorrelationId header.";
            }
            else if (expectedCorrelationId != null && props.CorrelationId != expectedCorrelationId)
            {
                errorMessage = string.Format("CorrelationId mismatch (expected '{0}').", expectedCorrelationId);
            }
            else
            {
                return true;
            }
            _logger.Error("Invalid message received. {0} Dropping message.", errorMessage);
            return false;
        }

        public void Dispose()
        {
            if (_bus != null)
            {
                Close();
            }
        }
    }
}
