# Rebus 2

![](https://raw.githubusercontent.com/rebus-org/Rebus/master/artwork/little_rebusbus2_copy-200x200.png)

[![install from nuget](https://img.shields.io/nuget/v/Rebus.svg?style=flat-square)](https://www.nuget.org/packages/Rebus)

This repository contains Rebus "core". You may also be interested in one of [the many integration libraries](https://github.com/rebus-org?utf8=%E2%9C%93&q=rebus.).


What?
====

Rebus is a lean service bus implementation for .NET, similar in nature to [NServiceBus][3] and [MassTransit][4], only leaner.

These are the goals - Rebus should have:

* a simple and intuitive configuration story
* a few well-selected options
* no doodleware
* dependency only on .NET ~~4.5~~ **Core**
* integration with external dependencies via small and dedicated projects
* the best error messages
* a frictionless getting-up-and-running-experience

and in doing this, Rebus should align very well with the NServiceBus way of doing things, which I like, thus allowing users (myself included) to easily migrate to NServiceBus at some point in a project's lifetime if Rebus for some reason falls short (which I don't think it will).

Oh, and Rebus is free as in beer and speech.

Why?
====

Because I wanted to build the .NET service bus that I would have the patience to work with every day, probably for several years to come. And I can be very impatient with my tools, so the most solemn goal of Rebus is that it should stay out of my way - and I think it does that just right!

If you want to read more, check out [the official Rebus documentation wiki][5] or check out [my blog][6].

One day, maybe I'll tweet something as well... [@mookid8000][2]

How?
====

Rebus is a simple .NET library, and everything revolves around the `RebusBus` class. One way to get Rebus up and running, is to manually go

	var bus = new RebusBus(...);
	bus.Start(1); //< 1 worker thread

	// use the bus for the duration of the application lifetime

	// remember to dispose the bus when your application exits
	bus.Dispose();

where `...` is a bunch of dependencies that vary depending on how you want to send/receive messages etc. Another way is to use the configuration API, in which case you would go

    var someContainerAdapter = new BuiltinHandlerActivator();

for the built-in container adapter, or

    var someContainerAdapter = new AdapterForMyFavoriteIocContainer(myFavoriteIocContainer);

to integrate with your favorite IoC container, and then

	Configure.With(someContainerAdapter)
		.Logging(l => l.Serilog())
		.Transport(t => t.UseMsmq("myInputQueue"))
		.Routing(r => r.TypeBased().MapAssemblyOf<SomeMessageType>("anotherInputQueue"))
		.Start();

	// have IBus injected in application services for the duration of the application lifetime

	// let the container dispose the bus when your application exits
	myFavoriteIocContainer.Dispose();

which will stuff the resulting `IBus` in the container as a singleton and use the container to look up message handlers. Check out the Configuration section on [the official Rebus documentation wiki][5] for more information on how to do this.

License
====

Rebus is licensed under [The MIT License (MIT)][1]. Basically, this license grants you the right to use Rebus in any way you see fit. See [LICENSE.md](/LICENSE.md) for more info.

[1]: https://opensource.org/licenses/MIT
[2]: https://twitter.com/#!/mookid8000
[3]: http://particular.net/nservicebus
[4]: http://masstransit-project.com/
[5]: https://github.com/rebus-org/Rebus/wiki
[6]: http://mookid.dk/oncode/rebus

[//]: [![downloads](http://img.shields.io/nuget/dt/Rebus.svg?style=flat-square)](https://www.nuget.org/packages/Rebus)
