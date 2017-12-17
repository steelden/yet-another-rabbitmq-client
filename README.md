# Yet Another RabbitMQ Client

Yes, this is another RabbitMQ client.
It was made to simplify messaging between parts of application and abstract away RabbitMQ-specific code.

Library is based on [EasyNetQ](https://github.com/EasyNetQ/EasyNetQ).

Depends on `EasyNetQ`, `RabbitMQ.Client`, `Newtonsoft.Json` and `NLog`.

### Warning

This library wasn't updated for quite a while and was released for nostalgic purposes.
Use at your own risk.

### Examples

**Basic configuration for fire & forget messaging:**

1. Connection
```c#
var connection = XBus.Initialize(c => c
  .LoadBusConnectionString("IntegrationBus")
  .AddMessageType<DoSomethingCommand>()
  .AddMessageType<SomethingHappenedEvent>()
);
connection.Connect();
...
connection.Close();
```
Somewhere in your `App.config`:
```
<connectionStrings>
  <add name="IntegrationBus" connectionString="host=localhost;virtualHost=/;username=rabbitmq;password=rabbitmq;requestedHeartbeat=10" />
</connectionStrings>
```

2. Send command
```c#
var command = new DoSomethingCommand() { };
XBus.Instance.SendCommand("do.something.command.queue", command);
```

3. Send event
```c#
var evt = new SomethingHappenedEvent() { };
XBus.Instance.SendEvent("something.happened.event.queue", evt);
```

4. Receive command/event
```c#
public class CoolStuffHandler : ICommandHandler<DoSomethingCommand>, IEventHandler<SomethingHappenedEvent>
{
  public Task HandleAsync(DoSomethingCommand message)
  {
    XBus.Instance.SendEvent("something.happened.event.queue", new SomethingHappenedEvent() { });
    return Task.FromResult(0);
  }
  public async Task HandleAsync(SomethingHappenedEvent message)
  {
    await Task.Delay(100);
    Console.WriteLine("Hello from event handler");
  }
}
```

**More advanced configuration for RPC request/reply:**

1. Send RPC requests
```c#
var msg = new SomeRpcRequest() { };
await XBus.Instance.SendRpcRequest("rpc.get.some.data.queue", msg, x =>
{
  x.On<SomeRpcResponse1>((response, continueAction) =>
  {
    if (response.Validate())
    {
      if (!response.IsCompleted) continueAction(null);
      Console.WriteLine("Hello from SomeRpcResponse1 handler");
    }
    return Task.FromResult(0);
  });
  x.On<SomeRpcResponse2>((response, continueAction) =>
  {
    if (response.Validate())
    {
      if (!response.IsCompleted) continueAction(null);
      Console.WriteLine("Hello from SomeRpcResponse2 handler");
    }
    return Task.FromResult(0);
  });
});

```


2. Serve RPC requests
```c#
XBus.Instance.ReceiveRpcRequest("rpc.get.some.data.queue", x => x.On<SomeRpcRequest>(HandleRequestAsync));
```

```c#
private async Task HandleDataRequestAsync(SomeRpcRequest message, Action<object> replyAction)
{
  var msg = new SomeRpcResponse1() { };
  replyAction(msg);
  var msg = new SomeRpcResponse1() { IsCompleted = true };
  replyAction(msg);
  var msg = new SomeRpcResponse2() { };
  replyAction(msg);
  var msg = new SomeRpcResponse2() { IsCompleted = true };
  replyAction(msg);
}
```


**Advanced configuration for high level multi-part paged messaging:**

1. Connection
```c#
XBus.Initialize(c => c
  .LoadBusConnectionString("IntegrationBus")
  .AddMessageTypes(_messages)               // register message types
  .AddExtApiModule(new SomeExtApiModule())  // register IExtApiIntegrationModule
);
XBus.Connect();
...
XBus.Close();
```

`ExtApi` modules can register a bunch of providers:
```c#
public class SomeExtApiModule : IExtApiModule
{
  public void RegisterProviders(IExtApiConfiguration config)
  {
    var provider = new SomeExtApiProviderWithParams();
    config.RegisterProvider<SomeDataObjectsRequest>("some", "objects", "data", provider); // providerName/objectName/action
    var provider2 = new SomeParameterlessExtApiProvider();
    config.RegisterProvider("some", "reports", "list", provider2); // providerName/objectName/actionName
    config.RegisterProvider("some", "reports", "data", provider2); // providerName/objectName/actionName
  }
}
```

3. Provider with additional parameters
```c#
public class SomeExtApiProviderWithParams : IExtApiDataProvider<SomeDataObjectsRequest>
{
  public async Task<IExtApiDataGenerator> HandleRequestAsync(string providerName, string objectName, string action, string id, SomeDataObjectsRequest @params)
  {
    return new SomeObjectsDataGenerator(@params);
  }
}
```

4. Provider without parameters
```c#
public class SomeParameterlessExtApiProvider : IExtApiDataProvider
{
  public async Task<IExtApiDataGenerator> HandleRequestAsync(string providerName, string objectName, string action, string id)
  {
    if (action == "list") return new SomeReportsObjectsListGenerator();         // /some/reports/list
    else if (action == "data") return new SomeReportsObjectsDataGenerator(id);  // /some/reports/data/123
    throw new InvalidOperationException("Unknown action.");
  }
}
```

5. Generators

Simple 1 page 1 record generator
```c#
public class SomeReportsObjectsListGenerator : IExtApiDataGenerator
{
  public int RecordsPerPart => 1
  public int TotalParts => 1
  public SomeReportsObjectsListGenerator() { }
  public async Task<string> GetPart(int partNo)
  {
    return JsonConvert.Serialize(new ReportsList { ... });
  }
  public void Dispose() { }
}
```

Generator for streaming paged data from database
```c#
public class SomeObjectsDataGenerator : IExtApiDataGenerator
{
  public int RecordsPerPart { get; private set; }
  public int TotalParts { get; private set; }
  public SomeObjectsDataGenerator(SomeDataObjectsRequest request)
  {
    ... start database query ...
    RecordsPerPart = 50;
    TotalParts = (TotalRecords + RecordsPerPart - 1) / RecordsPerPart; // expression "(a + b - 1) / b" rounds up to the nearest integer
  }
  public async Task<string> GetPart(int partNo)
  {
    ... load RecordsPerPart records from query ...
    return JsonConvert.Serialize(new ReportsData { ... });
  }
  public void Dispose()
  {
    ... close database query ...
  }
}
```

