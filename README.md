# Rebus.ServiceProvider.Named

This library allows you to use multiple Rebus instances in a single application context. This enables you to send and receive messages using different transports, configuration, etc. and also be able to bridge sending to another bus.

## Installation

Install Rebus.ServiceProvider.Named via the Nuget package manager or `dotnet` cli.

```powershell
dotnet add package Rebus.ServiceProvider.Named
```

---

[![Build status](https://ci.appveyor.com/api/projects/status/le58dnp0bcmdcsiq/branch/master?svg=true)](https://ci.appveyor.com/project/skwasjer/rebus-serviceprovider-named)
[![Tests](https://img.shields.io/appveyor/tests/skwasjer/rebus-serviceprovider-named/master.svg)](https://ci.appveyor.com/project/skwasjer/rebus-serviceprovider-named/build/tests)
[![codecov](https://codecov.io/gh/skwasjer/Rebus.ServiceProvider.Named/branch/master/graph/badge.svg)](https://codecov.io/gh/skwasjer/Rebus.ServiceProvider.Named)

| | | |
|---|---|---|
| `Rebus.ServiceProvider.Named` | [![NuGet](https://img.shields.io/nuget/v/Rebus.ServiceProvider.Named.svg)](https://www.nuget.org/packages/Rebus.ServiceProvider.Named/) [![NuGet](https://img.shields.io/nuget/dt/Rebus.ServiceProvider.Named.svg)](https://www.nuget.org/packages/Rebus.ServiceProvider.Named/) | Support for multiple named Rebus instances |

## Usage

### IBus vs ITypedBus<>

This library introduces a new bus interface, derived from `IBus`. The application code may have to be slightly modified in order to be able to mix and differentiate between multiple buses, depending on the use case.

Below are the differences when resolving either bus type both inside and outside message context:

| Bus type      | Description                                                           | In message context | Outside of message context |
| ------------- | --------------------------------------------------------------------- | ------------------ | -------------------------- |
| `IBus`        | Rebus' own bus interface                                                 | Yes                | No*                         |
| `ITypedBus<>` | Can be used to inject a specific bus instance by using a marker type | Yes                | Yes                        |

> \* This library changes the way the `IBus` type can be resolved from the dependency injection container (it does not change the functional implementation however). See below for more information why it can no longer be used outside of message context.

### `INamedBusFactory`

With multiple buses one obvious problem is that is no longer possible to request a specific instance of `IBus` or `IBusStarter`, at least, not outside of the scope of a Rebus handler (more on this later).

To solve that problem, this library adds a few new registration extensions, similar to `.AddRebus()`:

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Register using a name.
        services.AddNamedRebus("sqs-bus", c => c
            // F.ex. configure SQS transport
        );
    }
}
```

By registering the bus with a name, you can then the `INamedBusFactory` and request the bus instance by name:

```csharp
[ApiController]
public class MyController : ControllerBase
{
    private IBus _sqsBus;

    public MyController(INamedBusFactory namedBusFactory)
    {
        _sqsBus = _namedBusFactory.Get("sqs-bus");
    }

    public Task<IActionResult> PostAsync()
    {
        return _sqsBus.Send(new MyMessage());
    }
}
```

### `ITypedBus<>`

While using the factory gives you direct access to all bus instances by name, it is somewhat harder to write unit tests, since the factory now has to be mocked. You can therefor also choose to register a bus as a typed bus which can then be resolved as a `ITypedBus<>`:

```csharp
// Bus name marker class.
public class SnsBus { }

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Register using a type marker.
        services.AddTypedRebus<SnsBus>(c => c
            // F.ex. configure Sns transport
        );
    }
}
```

Using a typed bus, you can request it using dependency injection directly:

```csharp
public class MyService
{
    private ITypedBus<SnsBus> _snsBus;

    public MyService(ITypedBus<SnsBus> snsBus)
    {
        _snsBus = snsBus;
    }
}
```

You can also still request the bus by name however:

```csharp
// Resolves the same bus but by name
IBus snsBus = _namedBusFactory.Get(nameof(SnsBus));
```

### Message context and injecting owning IBus

As mentioned before, injecting the `IBus` outside of a message context is no longer possible when using this library, due to the the fact that the dependency injection container no longer knows which instance to inject.

It is still possible however to inject a bus of type `IBus` inside of a message context, simply because of the fact that the message is coming from a specific bus instance:

```csharp
public class MyMessageHandler : IHandleMessages<MyMessage>
{
    public MyMessageHandler(IBus bus)
    {
        // 'bus' is the instance that MyMessage is consumed from.
    }
}
```

### A complete example

```csharp
// Bus name marker class.
public class SnsBus { }

public class MyMessage { }
public class MyMessageProcessed { }

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<Service1>(); // For controller

        services
            // Register handler implementations.
            .AddRebusHandler<Service1>();

        // Register using a name.
        services.AddNamedRebus("sqs-bus", c => c
        // F.ex. configure SQS transport
        );

        // Register using a type marker.
        services.AddTypedRebus<SnsBus>(c => c
        // F.ex. configure Sns transport
        );
    }

    public void Configure(IApplicationBuilder app)
    {
        // Start both buses.
        app.UseNamedRebus("sqs-bus");
        app.UseTypedRebus<SnsBus>(bus => bus.Advanced.SyncBus.Subscribe(typeof(MyMessageProcessed)));
    }
}

[ApiController]
public class MyServiceController : ControllerBase
{
    private readonly Service1 _service;

    public MyServiceController(Service1 service)
    {
        _service = service;
    }

    public async Task<IActionResult> PostAsync()
    {
        await _service.StartLongProcess();
        return Accepted();
    }
}

public class Service1 : IHandleMessages<MyMessage>, IHandleMessages<MyMessageProcessed>
{
    private readonly ITypedBus<SnsBus> _snsBus;
    private readonly IBus _sqsBus;

    public Service1(INamedBusFactory busFactory, ITypedBus<SnsBus> snsBus)
    {
        _sqsBus = busFactory.Get("sqs-bus");
        _snsBus = snsBus;
    }

    public Task StartLongProcess()
    {
        // Sending a command to SQS.
        return _sqsBus.Send(new MyMessage());
    }

    public Task Handle(MyMessage message)
    {
        // Received via SQS bus, but we're publishing event to SNS bus.
        return _snsBus.Publish(new MyMessageProcessed());
    }

    public Task Handle(MyMessageProcessed message)
    {
        // Received through SNS, message has been processed.
        return Task.CompletedTask;
    }
}
```

## Caveats / limitations

The transaction context is not shared between multiple bus instances, and thus a message sent to another bus instance from inside a message handler may already be in-flight since that bus is not enlisted in the same transaction context. There is not much that can be done from the perspective of this library. The author of message handlers should carefully consider idempotency and more-than-once message delivery semantics.


## More info

### Supported .NET targets
- .NET 5.0
- .NET Standard 2.1/2.0

### Build requirements
- Visual Studio 2019
- .NET 5 SDK

#### Contributions
PR's are welcome. Please rebase before submitting, provide test coverage, and ensure the AppVeyor build passes. I will not consider PR's otherwise.

#### Contributors
- skwas (author/maintainer)
