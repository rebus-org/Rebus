# Rebus

![](https://raw.githubusercontent.com/rebus-org/Rebus/master/artwork/little_rebusbus2_copy-200x200.png)

Latest stable: [![NuGet stable](https://img.shields.io/nuget/v/Rebus.svg?style=flat-square)](https://www.nuget.org/packages/Rebus)

Current prerelease: [![NuGet pre](https://img.shields.io/nuget/vpre/Rebus.svg?style=flat-square)](https://www.nuget.org/packages/Rebus)

Tests: [![Build status](https://ci.appveyor.com/api/projects/status/gk13466i0o57o4rp?svg=true)](https://ci.appveyor.com/project/mookid8000/rebus)

This repository contains Rebus "core". You may also be interested in one of [the many integration libraries](https://github.com/rebus-org?utf8=%E2%9C%93&q=rebus.). 

For information about the commercial add-on (support, tooling, etc.) to Rebus, please visit [Rebus FM's page about Rebus Pro](https://rebus.fm/rebus-pro/).


What?
====

Rebus is a lean service bus implementation for .NET. It is what ThoughtWorks in 2010 called a 
["message bus without smarts"](https://www.thoughtworks.com/radar/tools/message-buses-without-smarts) - a library 
that works well as the "dumb pipes" when you need asynchronous communication in your microservices that follow
the ["smart endpoints, dumb pipes"](https://martinfowler.com/articles/microservices.html#SmartEndpointsAndDumbPipes) 
principle.

Rebus aims to have

* a simple and intuitive configuration story
* a few well-selected options
* no doodleware
* as few dependencies as possible (currently only [JSON.NET][JSON])
* a broad reach (targets .NET Standard 2.0, i.e. .NET Framework 4.6.1, .NET Core 2, and .NET 5 and onwards)
* integration with external dependencies via small, dedicated projects
* the best error messages in the world
* a frictionless getting-up-and-running-experience

and in doing this, Rebus should try to align itself with common, proven asynchronous messaging patterns.

Oh, and Rebus is FREE as in beer 🍺 and speech 💬, and it will stay that way forever.

More information
====

If you want to read more, check out [the official Rebus documentation wiki][REBUS_WIKI] or check out [my blog][REBUS_PAGE_ON_BLOG].

You can also follow me on Twitter: [@mookid8000][MOOKID8000_ON_TWITTER]

Getting started
====

Rebus is a simple .NET library, and everything revolves around the `RebusBus` class. One way to get Rebus
up and running, is to manually go

```csharp
var bus = new RebusBus(...);
bus.Start(1); //< 1 worker thread

// use the bus for the duration of the application lifetime

// remember to dispose the bus when your application exits
bus.Dispose();
```

where `...` is a bunch of dependencies that vary depending on how you want to send/receive messages etc.
Another way is to use the configuration API, in which case you would go


```csharp
var someContainerAdapter = new BuiltinHandlerActivator();
```

for the built-in container adapter, or

```csharp
var someContainerAdapter = new AdapterForMyFavoriteIocContainer(myFavoriteIocContainer);
```

to integrate with your favorite IoC container, and then

```csharp
Configure.With(someContainerAdapter)
    .Logging(l => l.Serilog())
    .Transport(t => t.UseMsmq("myInputQueue"))
    .Routing(r => r.TypeBased().MapAssemblyOf<SomeMessageType>("anotherInputQueue"))
    .Start();

// have IBus injected in application services for the duration of the application lifetime    

// let the container dispose the bus when your application exits
myFavoriteIocContainer.Dispose();
```

which will stuff the resulting `IBus` in the container as a singleton and use the container to look up
message handlers. Check out the Configuration section on [the official Rebus documentation wiki][REBUS_WIKI] for
more information on how to do this.

If you want to be more specific about what types you map in an assembly, such as if the assembly is shared with other code you can map all the types under a specific namespace like this:

```csharp
Configure.With(someContainerAdapter)
    .(...)
    .Routing(r => r.TypeBased().MapAssemblyNamespaceOf<SomeMessageType>("namespaceInputQueue"))
    .(...);

// have IBus injected in application services for the duration of the application lifetime    

// let the container dispose the bus when your application exits
myFavoriteIocContainer.Dispose();
```


License
====

Rebus is licensed under [The MIT License (MIT)][MITLICENSE]. Basically, this license grants you the right to use
Rebus in any way you see fit. See [LICENSE.md](/LICENSE.md) for more info.

The purpose of the license is to make it easy for everyone to use Rebus and its accompanying integration
libraries. If that is not the case, please get in touch with [hello@rebus.fm](mailto:hello@rebus.fm)
and then we will work something out.


[MITLICENSE]: https://raw.githubusercontent.com/rebus-org/Rebus/batches/LICENSE.md
[MOOKID8000_ON_TWITTER]: https://twitter.com/mookid8000
[REBUS_WIKI]: https://github.com/rebus-org/Rebus/wiki
[REBUS_PAGE_ON_BLOG]: http://mookid.dk/oncode/rebus

[JSON]: https://github.com/JamesNK/Newtonsoft.Json

[//]: [![downloads](http://img.shields.io/nuget/dt/Rebus.svg?style=flat-square)](https://www.nuget.org/packages/Rebus)
