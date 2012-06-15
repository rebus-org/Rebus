What?
====

Rebus is a lean service bus implementation, similar in nature to [NServiceBus][3] and [MassTransit][4].

These are the goals - Rebus should have:

* a simple and intuitive configuration story
* a few well-selected options
* no doodleware
* dependency only on .NET 4 BCL
* integration with external dependencies via small and dedicated projects
* the best error messages
* a frictionless getting-up-and-running-experience

and in doing this, I want Rebus to align very well with NServiceBus, allowing users (myself included) to easily migrate to NServiceBus at some point in a project's lifetime, if Rebus for some reason falls short.

Oh, and Rebus is free as in beer and speech.

Why?
====

I used to be a happy NServiceBus user, and I'm still using NServiceBus on some projects. It just puzzles me that 

* NServiceBus is 60 KLOC spread across 200+ projects - the code is hard to read
* Errors are often hard to diagnose
* Messing up during configuration yields weird errors at best, and no warnings or signs of things being wrong at worst
* NServiceBus went from being absolutely free to be licensed

I realize that NServiceBus is pretty cheap when you think of all the good things it can do for you, but to many people I think the license fee is an annoyance that will hinder them in introducing NServiceBus in all of their awesome projects - Which is a shame!

Then why don't I just use MassTransit then? Well, I wanted to do that, but I had so much trouble figuring out the philosophy of the MassTransit project that I gave up learning how to use it. I don't like how it needs a central runtime service to manage subscriptions, and I had a hard time figuring out how to make it work. And then there's the option of using PGM over MSMQ, but it just didn't work how I wanted it to work. Long story short: Too hard to get started!

Therefore, I wanted to try building a simple alternative to NServiceBus. Mainly as a personal research project, but also for myself to use in projects so I don't have to worry about licensing.

If you want to read more, check out [the official Rebus documentation wiki](https://github.com/mookid8000/Rebus/wiki) or check out [my blog](http://mookid.dk/oncode/rebus).

One day, maybe I'll tweet something as well... [@mookid8000][2]

How?
====

Rebus is a simple .NET library, and everything revolves around the `RebusBus` class. One way to get Rebus up and running, is to manually go

	var bus = new RebusBus(...);
	bus.Start();

where `...` is a bunch of dependencies that vary depending on how you want to send/receive messages etc. Another way is to use the configuration API, in which case you would go

	Configure.With(someContainerAdapter)
		.Logging(l => l.Log4Net())
		.Transport(t => t.UseMsmqAndGetInputQueueNameFromAppConfig())
		.DetermineEndpoints(d => d.FromRebusConfigurationSection())
		.CreateBus()
		.Start();

which will stuff the resulting `IBus` in the container as a singleton and use the container to look up message handlers. Check out the Configuration section on [the official Rebus documentation wiki](https://github.com/mookid8000/Rebus/wiki) for more information on how to do this.

License
====

Rebus is licensed under [Apache License, Version 2.0][1]. Basically, this license grants you the right to use Rebus however you see fit.

[1]: http://www.apache.org/licenses/LICENSE-2.0.html
[2]: http://twitter.com/#!/mookid8000
[3]: http://nservicebus.com/
[4]: http://masstransit-project.com/