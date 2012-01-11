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

I am a happy NServiceBus user, and I still am. It just puzzles me that 

* NServiceBus is 60 KLOC spread across 200+ projects - the code is hard to read
* Errors are often hard to diagnose
* Messing up during configuration yields weird errors at best, and no warnings or signs of things being wrong at worst
* NServiceBus went from being absolutely free to be licensed

I realize that NServiceBus is pretty cheap when you think of all the good things it can do for you, but to many people I think the license fee is an annoyance that will hinder them in introducing NServiceBus in all of their awesome projects - Which is a shame!

Then why don't I just use MassTransit then? Well, I wanted to do that, but I had so much trouble figuring out the philosophy of the MassTransit project that I gave up learning how to use it. I don't like how it needs a central runtime service to manage subscriptions, and I had a hard time figuring out how to make it work. And then there's the option of using PGM over MSMQ, but it just didn't work how I wanted it to work. Long story short: Too hard to get started!

Therefore, I wanted to try building a simple alternative to NServiceBus. Mainly as a personal research project, but also for myself to use in projects so I don't have to worry about licensing.

More info coming soon at http://mookid.dk/oncode/rebus

One day, maybe I'll tweet something as well... [@mookid8000][2]

How?
====

Pretty clunky at the moment, I'm sorry... haven't gotten into the configuration API story yet. Right now, this is how you get going with Rebus:

First, decide how you want to `ISendMessages` and `IReceiveMessages` - Rebus has something that can do both: `MsmqMessageQueue` - therefore:

    var msmq = new MsmqMessageQueue("service_input_queue");

Then, decide how subscriptions and sagas are to be stored - let's be serious about this:

	var connectionString = "data source=.;initial catalog=rebus_subscriptions;integrated security=sspi";
    var subscriptionStorage = new SqlServerSubscriptionStorage(connectionString, "subscriptions");
	var sagaPersister = new SqlServerSagaPersister(connectionString, "saga_index", "sagas");

Now, figure out how to go from `TMessage` to instances of something that implements `IHandleMessages<TMessage>`. This is where you'd probably insert your favorite IoC container. Let's pretend that I implemented `IActivateHandlers` in a `CastleWindsorHandlerActivator` (it's only two methods) - that would allow me to do this:

	var container = GetWindsorContainerFromSomewhere();
	var handlerActivator = new CastleWindsorHandlerActivator(container);

Now, figure out how a given message type should be mapped to the name of the endpoint that owns that message type - you do that by implementing `IDetermineDestination` (it's one single method that maps from `Type` to `string`) - if I'm OK with specifying it with the NServiceBus syntax (i.e. the `<UnicastBusConfig>` element from an NServiceBus app.config), I can use `DetermineDestinationFromNServiceBusEndpointMappings`:

	var endpointMapper = new DetermineDestinationFromNServiceBusEndpointMappings();

Now, figure out how to `ISerializeMessages` - at the moment there's `BinaryMessageSerializer` and `JsonMessageSerializer`:

	var serializer = new JsonMessageSerializer();

Lastly, think about whether some types of handlers should be invoked first as each handler pipeline gets executed... if that is not the case, just use

	var inspector = new TrivialPipelineInspector();

and NOW we're ready to create the bus:

	var bus = new RebusBus(handlerActivator, 
						   msmq, msmq, 
						   subscriptionStorage, sagaPersister, 
						   endpointMapper, serializer, inspector);

That created it. Let's make the bus start receiving messages:

	bus.Start();

If you've used NServiceBus, lots of things will immediately make sense with Rebus - everything about sending, publishing, subscribing, etc is the same. That also means that you can use all of your awesome NServiceBus skills with Rebus.

Well, that was a teaser. More stuff coming up some time in the future. I know that `RebusBus` has a pretty big constructor, but that's by design ;)

License
====

Rebus is licensed under [Apache License, Version 2.0][1]. Basically, this license grants you the right to use Rebus however you see fit.

[1]: http://www.apache.org/licenses/LICENSE-2.0.html
[2]: http://twitter.com/#!/mookid8000
[3]: http://nservicebus.com/
[4]: http://masstransit-project.com/