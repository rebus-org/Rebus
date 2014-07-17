# Changelog

## < 0.31.0

* Made Rebus - BAM!1

## 0.31.0

* Changed batch API to use `IEnumerable` instead of `params object[]` because the params thing could easily blur which would actually end up as a logical message.

## 0.31.1

* Made Rebus control bus messages `[Serializable]` because for some reason `BinaryFormatter` just likes it that way - applying the attribute makes instances of the class MUCH more serializable.

## 0.32.0

* Updated RabbitMQ client dependency

## 0.32.1

* Fixed handling of exceptions when committing user-provided unit(s) of work

## 0.32.2

* IEnumerable/Add API on SagaFixture
* SQL index on saga_id in saga index table

## 0.32.3

* Re-introduced automatic error queue creation when using RabbitMQ

## 0.32.4

* Fixed bug in NLogLoggerFactory that would try to use RebusConfigurer as a logger (?!)

## 0.32.5

* Added ability to customize max retries for specific exception types

## 0.33.0

* Removed (default) transaction scope around message handlers, made it configurable with `.Behavior(b => b.HandleMessagesInsideTransactionScope())`
* Added hybrid saga persister that can use different concrete persisters depending on the type of saga data

## 0.34.0

* Added (finally!) an icon for NuGet to display along with all Rebus packages
* New and improved Azure Service Bus topic-based transport implementation

## 0.34.1

* Better error handling when deferring messages
* Fixed bug with RabbitMQ transport that would result in exchange and error queue declaration too early, thus not adhering to customizations made by calling on the Rabbit MQ options

## 0.34.2

* Fixed bug in SQL saga persister and SQL subscription storage because SqlConnection is not nice enough to set the Transaction property when creating commands

## 0.34.3

* Fixed bug in `InMemorySagaPersister` that would not use the proper JSON serializer settings when deserializing

## 0.34.4

* Improved error reporting in case of exceptions while attempting to send all kinds of messages

## 0.34.5

* Added ability to let a special correlation ID header automatically flow to all outgoing messages when it is present

## 0.34.6

* Added ability to provide correlation ID on all outgoing messages automatically in cases where none was supplied from elsewhere ('elsewhere' being either explicitly specified or flowing from the current message context)

## 0.34.7

* Added configuration extension for Log4Net that will automatically set the correlation ID property on the ThreadContext

## 0.35.0

* Made JSON serializer handle encoding properly when deserializing messages
* Added Rebus.Async that extends Rebus with the ability to register reply handlers inline

## 0.35.1

* 1st go at implementing a SQL Server-based transport - can be used if you don't have MSMQ available on your machines
* Fixed glitch in error handling while doing any kind of send that threw an exception _without including the inner exception_

## 0.35.2

* Better handling of errors (i.e. DON'T IGNORE ERRORS) when MSMQ receive fails
* Optimization of SQL Server transport
* Fixed hard-to-find bug in how `SqlServerMessageQueue` would associate commands with the ongoing `SqlTransaction`

## 0.35.3

* Added transport performance showdown

## 0.35.4

* Only one SQL roundtrip to receive a message
* API to configure whether outgoing RabbitMQ messages should be persistent

## 0.35.5

* Special username header will flow like correlation ID if it is present

## 0.35.6

* Behavior option to allow for impersonating a proper user when the user name header is present on handled messages

## 0.36.0

* Updated Mongo stuff to use 1.8.1 driver and no deprecated APIs

## 0.36.1

* Update Mongo to 1.8.2 because that's the most recent version of the driver... duh!

## 0.37.0

* Added to `SqlServerMessageQueue` the ability to receive messages in prioritized order.

## 0.38.0

* Broke the Log4Net configuration API - sorry! But now it just adds the correlation ID to the thread context by default (i mean, why would you NOT do that?)

## 0.38.1

* Fixed constantly generated warning in timeout manager - thx [hagbarddenstore]

## 0.39.0

* Added ability to compress message bodies as well.

## 0.40.0

* Timeout manager SQL persistence oddity fixed: Explicit bigint PK instead of compound thing that could potentially lead to missed timeouts - thx [krivin]
* `IStartableBus` API extended with the ability to specify number of workers - thx [krivin]

## 0.40.1

* Added ability to use custom encoding with the built-in JSON serializer

## 0.41.0

* Upgraded RabbitMQ client dependency to 3.1

## 0.42.0

* Fixed logging when unit of work commit fails - should always be logged as a USER exception
* Fixed bug where adding custom headers could result in leaking memory in the form of numerous (dead) weak references

## 0.43.0

* Updated RabbitMQ dep to 3.1.5

## 0.43.1

* Rebus now always adds a unique Rebus Transport ID in the headers upon sending a message. This ID will stay the same when message is deferred, forwarded or sent to error queue.
* RabbitMQ transport uses the Rebus Transport ID as it's message ID, if it is not set otherwise
* RabbitMQ transport now initializes the input queue when subscribing - relevant if subscribe is called before receive

## 0.44.0

* Updated Log4Net dependency to 2.0.1 (BEWARE: It appears that the Log4Net public key has changed since previous version!!)
* Azure transport can now be configured in one-way client mode

## 0.44.1

* Avoid warning when disposing one-way Azure transport

## 0.44.2

* Added ability for RabbitMQ transport to NOT create the error queue. This way, the Rebus errorQueue setting just becomes the topic under which failed messages will be published.

## 0.44.3

* Fixed race condition bug when using RabbitMQ auto-delete input queue and subscribing on another thread after having started the bus

## 0.44.4

* Added ability for Azure Service Bus transport to actually use MSMQ when connection string is `UseDevelopmentStorage=true`

## 0.44.5

* Fixed bug where RabbitMQ transport would leak channels when used e.g. in combination with MSMQ in order to "bridge" from a MSMQ bus environment to a RabbitMQ environment

## 0.45.0

* Azure Service Bus transport now automatically creates the error queue

## 0.46.0

* Changed order of operations in MsmqMessageQueue that could lead to using an invalid transaction in rare circumstances
* Made all saga persisters treat `null` as a proper value, thus ensuring that saga data can be inserted, updated, and found with `null` in a correlation property

## 0.47.0

* Use hybrid stash model for transaction context, allowing it to overcome thread discontinuity in ASP.NET and WCF - thanks [jasperdk]
* Optimized data structure for attached headers by doing a hash code-based pre-lookup before searching for `WeakReference` target match

## 0.48.0

* Queue transaction failures are now properly caught and will be waited out when possible
* Increased Azure transport backoff times when throttling is detected

## 0.49.0

* When delivery tracking (i.e. the tracking of message IDs across multiple delivery attempts) times out, a WARN used to be logged. That is now an INFO because this scenario will be very common when running a set of workers as [competing consumers](http://www.eaipatterns.com/CompetingConsumers.html)
* Catch-and-rethrow `TargetInvocationException`s in message dispatch, and do some trickery to preserve the stack trace.
* The new catch-and-rethrow strategy allowed for properly including message IDs in the new `MessageHandleException` which is raised when a message cannot be handled.

## 0.50.0

* Sent messages are no longer logged at INFO level. Both sent and received messages are now logged at DEBUG level by calling ToString on the logical message in a logger called `MessageLogger`
* Dispose MSMQ messages after use - thanks [dev4ce]

## 0.50.1

* Fix that makes changelog 0.50.0 around ToString actually true

## 0.51.0

* Made timeout manager log internal errors properly - first as warnings, and if the problem persists for 1 minute it will be logged as an error
* Removed useless ON [PRIMARY] file group directives from SQL schema generation scripts

## 0.51.1

* Catch exception occurring while attempting to preserve the stack trace of a caught exception inside a `TargetInvocationException`. This will most likely be caused by the absence of a proper exception serialization constructor, which is really dumb -  oh, and thanks to the Unity container crew for making me realize how silly the need for serialization constructors is

## 0.52.0

* Updated NLog to 2.1.0

## 0.53.0

* Azure Service Bus transport: Set MaxDeliveryCount to 1000 to effectively disable built-in dead-lettering (because Rebus handles poison messages)
* Azure Service Bus transport: Peek lock defaults to 5 minutes (which is max)
* Azure Service Bus transport: Make an `Action` available in the message context under the `AzureServiceBusMessageQueue.AzureServiceBusRenewLeaseAction` key to allow for renewing the peek lock when performing long-running operations

## 0.53.1

* Avoid ending up overflowing the stack if `Console.WriteLine` fails

## 0.54.0

* Change `SqlServerSagaPersister` and `SqlServerSubscriptionStorage` to use the same `ConnectionHolder` as `SqlServerMessageQueue`
* Removed the hack that could automatically dig the `SqlTransaction` out of a `SqlConnection` (because it did not work all the time)
* Clean up a few things inside `AzureServiceBusMessageQueue`
* Added test to verify a scenario involving `AzureServiceBusMessageQueue` and `SqlServerSagaPersister`

## 0.54.1

* Nudged order of disposal and logging inside Azure Service Bus transport
* Warning when sending > 90 messages with Azure Service Bus transport + `InvalidOperationException` when > 100 (because of a limitation in Azure Service bus)

## 0.54.3

* `XmlSubscriptionStorage` automatically creates directory pointed to by the subscription XML file path - thanks [hagbarddenstore]
* Made the Rabbit transport throw out its subscription and underlying model when an end-of-stream is detected

## 0.54.4

* Nothing changed - pushed new version because NuGet.org had a seisure the other day and 0.54.3 wasn't properly uploaded

## 0.54.5

* Fixed `SqlServerSubscriptionStorage` to be able to work when publishing within a `TransactionScope` when it manages the connection by itself - thanks [jasperdk]

## 0.54.6

* Added load balancer NuGet package

## 0.54.7

* Fixed logging in load balancer

## 0.54.8

* Made `ConsoleLoggerFactory` public so it can be used e.g. from processes hosting the load balancer

## 0.55.0

* RabbitMQ client updated - thanks [hagbarddenstore]

## 0.55.1

* Fixed Rabbit transport nuspec

## 0.56.0

* Added ability to configure queue polling backoff strategy to low-latency mode - thanks [hagbarddenstore]

## 0.56.1

* Don't make so many DEBUG logging statements while backing off

## 0.57.0

* Tweaked ASB transport so that send batching kicks in only when there's 100 or more messages to send
* Fixed it so that the error log on a tracked message has the local time (i.e. machine time) as its timestamp, and not UTC

## 0.58.0

* Fixed it so that the `MarkedAsComplete` event is raised also when a piece of saga data was never persisted - before, it was tied to the `Deleted` event from the persister, which you not be raised if the saga data was not persistent.
* Made Rebus Timeout Service create a service dependency on local SQL Server/MongoDB if the connection is local. This way, services will be started/stopped in the right order. Thanks [caspertdk]
* Fixed it so that headers attached to deferred messages are preserved when roundtripping the timeout manager.

## 0.58.1

* Added 'CorrelationId' thread-local context variable to NLog logger, similar to how it's done with the Log4Net logger.

## 0.58.2

* Fixed `AttachHeader` bug in `FakeBus`.

## 0.59.0

* Fixed bug when working with automatic `TransactionScope` and sagas persisted in SQL Server - thanks [jasperdk]

## 0.60.0

* Made SQL Server saga persister behave more like expected by skipping null-valued properties in the saga index.

## 0.60.1

* Fixed but in SQL Server saga persister that could result in malformed SQL when there are no correlation properties at all.

## 0.61.0

* Made it configurable whether null-valued correlation properties should be included in the inde with SQL Server saga persister.

## 0.61.1

* Added to RavenDB saga persister the ability to obtain the current session from the outside, thereby allowing you to make the saga work part of the same RavenDB transaction that you're working in.

## 0.62.0

* Fixed leakage of SQL connections (and other potential issues) when using ambient transactions - thanks [mgayeski]

## 0.63.0

* Added several `Subscribe`/`Unsubscribe` overloads to `IRebusRouting` so you can `bus.Advanced.Routing.Subscribe(someMessageType)` if you want

## 0.63.1

* Added file system-based transport. Please do not use this one for your really important messages.
* Fixed MSMQ transaction leak - thanks [jasperdk]

## 0.64.0

* Updated MongoDB driver dependency to 1.9

## 0.64.1

* Added ability to configure JSON serializer to serialize enums with their string representations - thanks [maeserichar]

## 0.65.0

* Updated StructureMap dependency to 3.0 - thanks [fritsduus]

## 0.65.1

* Added Serilog logger - thanks [fritsduus]

## 0.66.0

* Fixed it so that inner exceptions are included when a `SqlException` causes saga persister to not be able to insert.
* Added Postgres persisters for sagas, subscriptions, and timeouts - thanks [hagbarddenstore]

## 0.67.0

* Added ability for RabbitMQ transport to publish to different exchanges instead of different routing keys - thanks [pruiz]
* Limit message body size to 32 MB for RabbitMQ transport because publishing larger messages can destabilize the server.

## 0.68.0

* Added additional routing options with RabbitMQ - endpoints can now be adressed on several forms: `topic`, `@exchange`, and `topic@exchange` - thanks [pruiz]

## 0.69.0

* Removed MSMQ error queue existence check when queue is remote (because it can't be done, and because it doesn't make sense for remote queues)
* Added ability for saga persisters to provide the ability to update more than one saga instance for an incoming message, by implementing `ICanUpdateMultipleSagaDatasAtomically` - thanks [PeteProgrammer]

## 0.70.0

* Made all batch operations obsolete so that you'll get a compiler warning if you use them.

## 0.70.1

* Added container adapter for SimpleInjector - thanks [oguzhaneren]


[pruiz]: https://github.com/pruiz
[hagbarddenstore]: https://github.com/hagbarddenstore
[fritsduus]: https://github.com/fritsduus
[maeserichar]: https://github.com/maeserichar
[jasperdk]: https://github.com/jasperdk
[mgayeski]: https://github.com/mgayeski
[caspertdk]: https://github.com/caspertdk
[dev4ce]: https://github.com/dev4ce
[krivin]: https://github.com/krivin
[PeteProgrammer]: https://github.com/PeteProgrammer
[oguzhaneren]: https://github.com/oguzhaneren
