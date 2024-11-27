﻿# Changelog

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

## 0.70.2
* Fix problem when trying to send Multicast message to error queue - thanks [maeserichar]

## 0.70.3
* Fixed vulnerability that could lead to executing arbitrary SQL statements by injecting it in a correlation property on a saga message when using SQL Server and PostgreSQL saga persisters.

## 0.71.0
* Fixed bug that could result in not automatically binding the error queue topic to the error queue when using RabbitMQ
* Update MongoDB driver dependency to 1.9.2
* Fixed error in build script that did not build the Serilog project. Also fixed it so that Rebus.Serilog is .NET 4 like the rest of Rebus.

## 0.71.1
* Ensure declaration of exchange when using `OneExchangePerType` - thanks [maeserichar]

## 0.71.2
* Fixed SQL Server schema creation issue where querying `sys.tables` would give an error if the database's current collation results in case sensitive table names - thanks [tiipe]

## 0.71.3
* Made SQL Server subscription storage & saga persister API accept connection string names (just like the SQL transport config)  - thanks [tiipe]

## 0.71.4
* Allow for specifying the messages table name on the SQL Server transport - thanks [tiipe]
* Modified SQL server-based persistence things to accept connection string _names_ as well - thanks [tiipe]
* Fixed subtle bug that would not remove the saga context from the current message context's items - thanks [dimajanzen]

## 0.72.0
* Added auditing capability - endpoints can now send a copy of the transport message to an audit queue whenever they have been successfully handled or published (which is the only two times when messages "disappear" - when handling a message, it "disappears", and when publishing to 0 subscribers, it also "disappears"). This can provide the basis for advanced tooling later on, e.g. for tracking correlation across systems, for gathering statistics etc. For now, go audit your messages - more tools will follow :)

## 0.73.0
* Updated RabbitMQ dependency - thanks [maxx1337]
* This version DOES NOT WORK because of the merge bug fixed in 0.75.0

## 0.74.0
* Upgraded to .NET 4.5 !!
* `IHandleMessagesAsync` introduced - allows for having `async` message handlers
* Fixed container adapters to correctly handle resolving async message handlers - thanks [arneeiri]
* This version DOES NOT WORK because of the merge bug fixed in 0.75.0

## 0.74.1
* Fixed bug that would not allow for configuring and starting a bus without an XML configuration section.
* This version DOES NOT WORK because of the merge bug fixed in 0.75.0

## 0.75.0
* Fixed merge bug that made published Rebus core assembly unusable!

## 0.75.1
* Fixed container adapter registration bug that would make it impossible to have `IMessageContext` injected into more than one handler in the pipeline. Also ensures that the container does not dispose the message context.

## 0.75.2
* Change SQL Server subscription storage command to be truly idempotent - thanks [tobiaxor]

## 0.76.0
* Auto-subscribing extension on `IBus` - thanks [seankearon]
* Updated Azure Service Bus dependency to 2.5.4

## 0.77.0
* Improved Serilog integration

## 0.77.1
* Made `PoisonMessageInfo` ctor public so that the `IErrorTracker` can actually be implemented outside of Rebus - thanks [bchavez]

## 0.78.0
* Added extension that allows for composing a chain-of-responsiblity-like way of determining message ownership - thanks [DixonD-git]
* Introduced a little bit of structure in the `Rebus.Extensions` package - might not be the final way, but it should do for now

## 0.78.1
* Added support for DryIoC - thanks [DixonD-git]

## 0.78.2
* Fixed DryIoc nuget package

## 0.79.0
* Changed `IStoreTimeouts` API to return a `DueTimeoutsResult` instead of an `IEnumerable<DueTimeout>`
* Made `SqlServerTimeoutStorage` grab row locks on found due timeouts, allowing for "competing timeout consumers"

## 0.80.0
* Changed default encoding of outgoing messages to be UTF8 instead of UTF7 when using the built-in JSON serializer
* Added `NewtonsoftJsonMessageSerializer` that is honest about its dependency on Newtonsoft JSON.NET and thus lets you customize the serialization settings - thanks [joshua5822]

## 0.80.1
* Support async initiation of sagas via `IAmInitiatedByAsync<>` - thanks [AndreaCuneo]

## 0.81.0
* Support for idempotent sagas via the `Rebus.IdempotentSagas` packages - thanks [pruiz]

## 0.82.0
* Updated MongoDB stuff to work with MongoDB 3/WiredTiger - thanks [caspertdk]

## 0.82.1
* Updated Serilog dep - thanks [tiipe]

## 0.83.0
* Fixed concurrent message send in file system transport - thanks [mgibas]

## 0.84.0
* Made Azure Service Bus transport stop the peek lock renewal timer when the transaction context goes into the commit/rollback/cleanup phase. Seems more right, and it avoids a pesky race that could result in confusing `MessageLockLostException`s if the message would happen to be successfully completed right before attempting to renew the peek lock.

## 0.90.0
* Refactored most of Rebus! NOTE: (very) breaking change!! - 0.90.0 version series will function as ALPHA and BETA of what will become Rebus 2.0.0
* Introduced pipelines for message send and receive
* New configuration API based on Injectionist
* Simpler transaction handling, simpler `ITransport` abstration
* Idempotent sagas
* Several new integrations: Jil, Azure Storage Queues, Amazon SQS
* Async to the core! (i.e. remember to `bus.Send(yourMessage).Wait()` if you cannot `await bus.Send(yourMessage)`)
* Routing based on topics - type-based routing ("Rebus Classic") is just a subset of this
* Batching features completely removed
* In-memory transport can be used for in-process communication and testing
* Ground laid for more advanced control of Rebus internals, e.g. to implement auto-scaling of number of workers, diagnostics, etc.
* Tests refactored to make it much much easier to introduce new implementations based on contract tests

## 0.90.1
* Added `TransactionScope` support via the `Rebus.TransactionScope` package (note: requires at least .NET 4.5.1 for `TransactionScopeAsyncFlowOption.Enabled` to be there)

## 0.90.2
* Added ability to storage subscriptions in a JSON file

## 0.90.3
* Added RavenDB persistence for subscriptions and timeouts - thanks [mclausen]

## 0.90.4
* Added experimental legacy compatility option (`.Options(o => o.EnableLegacyCompatibility())`)

## 0.90.5
* Made MSMQ transport work better in legacy mode

## 0.90.6
* Added ability to use external timeout manager

## 0.90.7
* Added some more headers to audited messages
* Added PostgreSQL persistence

## 0.90.8
* Added pub/sub support to legacy compatibility mode
* Changed `AzureServiceBusTransport` to work with topics when doing pub/sub which then avoids having to configure subscription storage when working with Azure Service Bus


## 0.90.9
* Separated error tracking out behind `IErrorTracker` interface, allowing for e.g. keeping error counts in a database somewhere, thus allowing for multiple parallel workers to not exceed maximum delivery attempts
* Added ability to auto-add headers to outgoing messages by using `[Header("some_headers", "some_value")]`

## 0.91.0
* Changed SQL persistence to work with `IDbConnectionProvider` as originally intended - thanks [MrMDavidson]
* Made Azure Service Bus transport use the native message deferral mechanism
* Made Azure Storage Queues transport use the native message deferral mechanism

## 0.92.0
* Made SQL Server transport use a `visible` field in the database to implement message deferral (warning: schema change)

## 0.92.1
* Fixed RabbitMQ NuGet dependency to explicitly depend on 3.5.4 because silly NuGet will default to the lowest version and that does not play well with strong naming and actually having versions on assemblies

## 0.92.2
* Fixed RabbitMQ transport bug that would create wrong queue bindings - please update to this version :)

## 0.92.3
* Changed `SqlTransport` to use the cryptic `DELETE OUTPUT` style query yielding ~ 10 times performance improvement - thanks [MrMDavidson] for a very well-documented PR :)

## 0.92.4
* Added one-way client mode + appropriate configuration extensions for all transports but Amazon SQS (it requires some more work)

## 0.93.0
* Updated MongoDB driver dependency to 2.0.1 and rewrote MongoDB persistence to take advantage of `async`/`await` - thanks [kevbite]

## 0.94.0
* Made the type-based pub/sub API default and moved the raw string-based topic API into `bus.Advanced.Topics`

## 0.95.0
* Re-introduced the previous Azure Service Bus implementation that relied only on queues to work - can be used with the Azure Service Bus Basic tier

## 0.96.0
* Moved the explicit routing function to an advanced routing API
* Added saga data snapshotting + snapshot storages for SQL Server and MongoDB
* Updated Newtonsoft JSON.NET dep to 7.0.1
* Added file system-based saga snapshot storage

## 0.97.0
* Change RabbitMQ transport to allow for using Rabbit's native pub/sub capabilities, thus removing the need for a subscription storage

## 0.98.0
* Updated SimpleInjector dependency to 3.0.1
* Added saga auditing mechanism that can output mutations to the log
* Added file system transport
* Updated Azure Service Bus dependency to 3.0.1 
* Updated Azure Storage dep to 5.0.0

## 0.98.1
* Set message label on sent MSMQ messages (is shown in MSMQ tools like Rebus Snoop etc.)

## 0.98.2
* Added transport message routing capability

## 0.98.3
* Made disposal of resources more consistent (still not entirely consistent though, depends on importance)

## 0.98.4
* Added `IsNew` property to `Saga` base class allowing saga handlers to easily determine whether the instance is new

## 0.98.5
* Added one-way client mode for Amazon SQS transport

## 0.98.6
* Added "per Rebus message" lifestyle to Windsor container configuration extensions

## 0.98.7
* Added simple 2nd level retries mechanism
* Added ability to forward the current transport message (optionally supplying some extra headers)
* Added configurable idle time backoff strategy

## 0.98.8
* Fixed bug that could result in double-resolving handler instances with Ninject, Castle Windsor, and Unity because of a lazy-evaluated `IEnumerable`

## 0.98.9
* Fixed MSMQ remote queue path formatting

## 0.98.10
* Extended built-in handler activator with `Register` overloads that allow for registering handler factory functions that accept `IMessageContext` and `(IBus, IMessageContext)` as arguments

## 0.98.11
* Added `IMessageContext.AbortDispatch()` which aborts dispatch of the current message to any handlers following the one currently being executed.

## 0.98.12
* Added ability to mark saga data as unchanged by calling `MarkAsUnchanged()` on the saga handler.

## 0.99.0
* Inject loggers. Will remove static logging things later.
* Allow for enabling partitioning with Azure Service Bus transports
* Changed Azure Service Bus transports to use streams as the message body to avoid silly XML-wrapping - thanks [meyce]
* Updated WindowsAzure.ServiceBus dependency to 3.0.4
* Updated WindowsAzure.Storage dependency to 6.0.0

## 0.99.1
* Added Serilog log event enricher + configuration extension for including Rebus' correlation ID in log events
* Added custom NLog layout renderer that outputs the correlation ID of the message currently being handled in the `${rebus-correlation-id}` variable

## 0.99.2
* Fixed NLog nuget dependency

## 0.99.3
* Access to more useful information on `Failed<TMessage>`

## 0.99.4
* Added no-inlining jit directive to the Windsor package's `AutoRegisterHandlersFromThisAssembly` method because it looks like handlers are sometimes not picked up as they should

## 0.99.5
* Replaced Windsor's assembly-scanning with manual scan

## 0.99.6
* Set correlation ID, content type, and label properties on brokered messages with Azure Service Bus transport

## 0.99.7
* Limit length of header values used when using Azure Service Bus transport

## 0.99.8
* Fixed it so that delivery will not be retried indefinitely on commit exceptions

## 0.99.9
* Changed RabbitMQ routing to use two exchanges: one for direct addressing, and one for pub/sub messaging. This way, "clean" RabbitMQ topics can be subscribed to and then used when publishing, allowing for subscribing to topics using wildcards.

## 0.99.10
* Fixed nuspec on StructureMap package, made SimpleInjector package dependency version criteria more tolerant, and updated the Unity dependency to v 4

## 0.99.11
* Added ability to immediately forward messages on certain exception types (optionally when those exceptions satisfy some predicate)

## 0.99.12
* Added LightInject container adapter - thanks [puzsol]

## 0.99.13
* Added DryIoc container adapter - thanks [dadhi]

## 0.99.14
* General handling of receive errors by logging as warning

## 0.99.15
* Changed back to logging full exception details in the final ERROR log event

## 0.99.16
* Added `EnlistRebus` option to `Rebus.TransactionScopes`, allowing for easily enlisting an ambient Rebus transaction in an ambient .NET transaction

## 0.99.17
* Added `IBus` API overloads for `Subscribe` and `Unsubscribe` that accept a `Type` as a parameter - thanks [gertjvr]
* Fixed `ExpiredMessagesCleanup` warning because of locked rows when handling message for a long time with the SQL Server transport
* Introduced `rbs2-defer-recipient` header to designate the queue to deliver the message to when it returns - allows for doing request/reply with a defer in the middle without losing the return address (which must be transferred manually, though, unless you `bus.Advanced.TransportMessage.Defer`)
* Crude attempt at allowing for sagas to resolve their conflicts by overriding `ResolveConflict`

## 0.99.18
* Fixed bug that would fail saga data auditing when the message could not be correlated with a saga data instance

## 0.99.19
* Fixed Rebus' SimpleInjector container adapter registrations to be able to resolve `IMessageContext` as part of the container's verification process

## 0.99.20
* Fixed bug in Amazon SQS transport that would silently ignore errors when sending messages - thanks [gertjvr]

## 0.99.21
* Small changes to Rebus' `TransactionScope` enlistment support

## 0.99.22
* Added RavenDB saga persister - thanks [bjomi]

## 0.99.23
* Fixed it so that SQL Server transport does not start the background "expired messages cleanup" task when it is a one-way client

## 0.99.24
* Fixed bug in in-mem saga storage that would accidentally enforce uniqueness of correlation properties across saga data types

## 0.99.25
* Fixed options configuration API to be more consistent and thus less prone to confusion - thanks [xenoputtss]
* Added experimental unit of work API

## 0.99.26
* Fixed bug in RavenDB saga persister - thanks [bjomi] and [jsvahn]
* Fixed bug in Injectionist that could lead to resolve instances sometimes now being properly initialized/disposed

## 0.99.27
* Added Wire serializer

## 0.99.28
* Automatically set correlation property on newly initiated saga data when possible (we try to do it when there's one single correlation property for the message, and ignore failure to do so)

## 0.99.29
* Fixed Rebus.MongoDb package (nuspec was lying about its MongoDB driver dependency)

## 0.99.30
* Removed hardcoded region settings in purge operation with SQS transport - thanks [gertjvr]
* Better error message when attempting to complete a message whose lock has been lost with ASB

## 0.99.31
* Added unit of work guards and fixed bug that would result in exceptions when providing no rollback/cleanup action when using the async version

## 0.99.32
* Fixed logging of the configured region with Amazon SQS transport - thanks [gertjvr]
* Added experiment in-process hosting of OWIN endpoints via the Rebus.Owin package

## 0.99.33
* Fixed bug where an exception thrown while correlating an incoming message with a saga would not be included as inner exception - thanks [mortenherman]

## 0.99.34
* Made small improvements to Rebus.Async (added timeout option + added ability to remove abandoned replied when their age exceeds a configurable threshold)

## 0.99.35
* Fixed bug that caused the Azure Service Bus transport to not be properly initialized when using the "Basic" tier - thanks [torangel]

## 0.99.36
* Added Protobuf serializer
* Much improved error message when sending to non-existent queue with Azure Service Bus
* Changed `Failed<TMessage>` to be `IFailed<TMessage>` (and thus an interface) in order to make it covariant
* Added Wire serializer configuration extensions

## 0.99.37
* Fixed subtle unintented behavior so that messages inherited from an initiating type can initiate sagas too
* Enabled true polymorphic correlation

## 0.99.38
* Fixed saga correlation bus introduced in 0.99.37

## 0.99.39
* Brough back `FakeBus` - an implementation of `IBus` that records everything that happens to it as events which you may query during the assert-phase of your tests
* Added `SagaFixture` to ease saga testing

## 0.99.40
* Added correlation sequence number as an auto-flowing header too - can be used to deduce causation
* Fixed bug that could accidentally overwrite the ID of a saga because of the nice auto-setter behavior

## 0.99.41
* Fixed RavenDB subscription and timeout storage

## 0.99.42
* Fixed bug in Rebus' protobuf serializer that caused it to be unable to serialize Rebus' control bus messages

## 0.99.43
* Updated StructureMap dependency to 4.1.2.386 - thanks [dougkwilson]
* Made isolation level and timeout configurable when using the `TransactionScope` support extension

## 0.99.44
* Added option for Azure Service Bus transport to skip queue creation to make it possible to use a read-only SAS key instead of a connection string with manage rights - thanks [torangel]
* Made due timeouts poll interval configurable
* Made RavenDB subscription storage much more tolerant towards concurrency exceptions when topic documents are experiencing contention

## 0.99.45
* Added ability to set up fake saga data in `SagaFixture`
* Added method to clear recorded events of `FakeBus`

## 0.99.46
* Better error behavior when deferring a message from a one-way client without setting the defer-recipient

## 0.99.47
* Moved defer-recipient header check to the pipeline, giving other pipeline steps a chance to set it

## 0.99.48
* Don't rethrow `ResolutionException` inside another `ResolutionException` in case of configuration errors - the stack trace reveals the call stack so nicely, there's no need to have 10 layers of exceptions too
* Marked topic-based routing configuration API as obsolete (type-based routing is perfectly capable of using raw topics if you like)
* Added fallback capability to the router (i.e. the ability to configure an endpoint that is used when none of the mappings match)
* Fixed bug in retry strategy step that could accidentally keep tracked message IDs for too long
* Prohibit sending `IFailed<TMessage>` anywhere (because it is most likely an error)

## 0.99.50
* Fixed concurrency issues in RavenDB saga storage - remember to use optimistic concurrency regardless of the configuration of the document store

## 0.99.51
* Updated RabbitMQ dependency to 3.6.1
* Updated Newtonsoft JSON.NET dependency to 8.0.3 across the board
* Implemented ability for RabbitMQ transport to skip exchange and queue declarations as well as input queue bindings
* Renamed `Rebus.AzureStorageQueues` NuGet package to `Rebus.AzureStorage` because it has integration things for Azure storage
* Made RabbitMQ transport automatically declare & bind destination queues when they're used the first time (to avoid sending a message into nowhere)

## 0.99.52
* Moved `.UseExternalTimeoutManager(...)` configuration method to the `.Timeouts(t => ...)` configurer because it more natural
* Added ability for JSON file-based subscription storage to be centralized
* Updated Azure Service Bus dependency to 3.2.0

## 0.99.53
* Extended polymorphic saga correlation to make proper use of implemented interfaces too

## 0.99.55
* Real pause (5 s) when the next transport message cannot be received
* Added ability to configure worker shutdown timeout to allow for longer-running tasks to finish - thanks [Rzpeg]

## 0.99.56
* Changed timeout settings on Azure Service Bus transport to respect the setting from the connection string - thanks [Rzpeg]
* Changed RabbitMQ transport to use `QueueingBasicConsumer` instead of polling - thanks [Hangsolow]
* Separated encryption algorithm out into injectable and thus configurable `IEncryptor` service - thanks [zabulus]
* Added data bus feature with data storages for SQL Server, the file system, and in-mem (for testing)
* Fixed issue with high disk activity when using MSMQ

## 0.99.57
* Added ability to specify custom client properties with RabbitMQ

## 0.99.58
* Updated LightInject to 4.0.9
* Updated StructureMap to 4.2.0.402
* Updated DryIoC to 2.5.0
* Updated Serilog to 1.5.14
* Updated NLog to 4.3.4
* Updated Log4net to 2.0.5
* Updated RavenDB client to 3.0.30115
* Added ability to explicitly configure in-mem subscription storage
* Added ability for in-mem subscription storage to share a subscriber store and become "centralized" (can be used in conjunction with in-mem transport to emulate in memory everything that can be achieved with a real multicast-enabled transport like RabbitMQ, Azure Service Bus, etc.)


## 0.99.59
* Fixed MSMQ mangled message receive so that the message is not "lost"
* Added cancellation support to the `Receive` operation of the transport - thanks [Rzpeg]

## 0.99.60
* Changed data type of the SQL Server saga storage `[data]` column to `VARBINARY(MAX)` because `NVARCHAR(MAX)` was extremely slow! (existing tables with `NVARCHAR(MAX)` data should still work fine - a simple schema check is made at startup)
* Added Azure blobs-based data bus storage

## 0.99.61
* Added ability configure prefetch count with RabbitMQ
* Added a few additional defensive RabbitMQ reconnection measures, like e.g. throw out prefetched messages if the connection is lost

## 0.99.62
* Added simple metadata API to data bus

## 0.99.63
* Maintain time of last read access in all data bus storages
* Made `DataBusAttachment` serializable with all currently supported serializers (JSON.NET, Jil, Wire, and Protobuf)

## 0.99.64
* Fixed potential locked-file issue when multiple readers are reading the same data with the data bus

## 0.99.65
* Added MSMQ transport configuration builder to allow for customizing the `MessageQueue`, e.g. by setting/changing permissions

## 0.99.66
* Added file system-based saga storage and timeout storage - thanks [jeffreyabecker]
* Added Azure tables-based subscription storage - thanks [jeffreyabecker]
* Added Azure tables-based timeout storage - thanks [jeffreyabecker]
* Added experimental Azure blobs- and tables-based saga storage - thanks [jeffreyabecker]
* Added Rebus message handler convention to the StructureMap integration - thanks [jeffreyabecker]
* Created `Rebus.Recipes` convenience package with helpful extensions for identity propagation and consistent persistence configurations - thanks [jeffreyabecker]
* Fixed subtle MSMQ receive blocked thread timing quirks
* Fixed exception on message deferral for very short intervals - thanks [jeffreyabecker]
* Added configuration API for using user-provided DB connection with SQL Server - thanks [zabulus]

## 0.99.67
* Added MsgPack serializer
* Updated Newtonsoft JSON.NET to 9.0.1 throughout
* Added ability to skip encryption of a particular message by adding the `rbs2-disable-encryption` header (can be found as a constant in `EncryptionHeaders.DisableEncryptionHeader`)
* Provide real `Exception`s in `IFailed<TMessage>`

## 0.99.68
* Provide optional Jil configuration `Options` parameter, allowing for customizing serialization settings - thanks [Rzpeg]
* Make RabbitMQ transport accept multiple connection strings separated by , or ; which will then be cycled on connection failures
* Fix very subtle bug in saga persisters that would result in sometimes loading saga data of the wrong type when having multiple saga handlers with different saga data types in the same endpoint, handling the same message, correlating by ID - affected persisters: Azure Storage, SQL Server, PostgreSQL, RavenDB, File System
* Fix subtle bug in how the ambient transaction context is picked up that would sometimes (when another bus is used from withing a message handler, and only with some transports) result in ending up doing `Send` on the other bus' transport

## 0.99.70
* Update AWS SDK version to latest (AWSSDK.Core 3.1.9.1, AWSSDK.SQS 3.1.0.12) - thanks [gertjvr]
* Make RabbitMQ transport tolerate incoming message without ID (try to assign from RabbitMQ ID and ultimately calculate Knuth hash from message body)

## 0.99.71
* Fix bug in RavenDB saga persister that would not properly guard saga data against race conditions in the face of concurrent updates
* Add thread pool-based worker factory that can be enabled by going `UseThreadPoolMessageDispatch` on the options configurer

## 0.99.72
* Respect worker shutdown timeout also when stopping the worker
* Back off slightly when there is no work to do

## 0.99.73
* Add GZIPping capability to data bus storage - can be enabled by attaching `.UseCompression()` in the data bus configuration builder
* Factor forwarding of failed messages to error queues out into `PoisonQueueErrorHandler` which implements `IErrorHandler`. Make room for customizing what to do about failed messages.

## 0.99.74
* Mark assemblies as CLS compliant becase VB.NET and F# programmers are most welcome too - thanks [NKnusperer]
* Update Serilog dependency to 2.1.0 - thanks [NKnusperer]
* Limit number of workers to match max parallelism
* Make thread pool-based workers default (old strategy can still be had by calling `o.UseClassicRebusWorkersMessageDispatch()`)
* Update NLog dependency to 4.3.7 - thanks [SvenVandenbrande]
* Update SimpleInjector dependency to 3.2.0 - thanks [SvenVandenbrande]
* Make adjustment to new thread pool-based workers that makes better use of async receive APIs of transports
* Update Wire dependency to 0.8.0
* Update Autofac dependency to 4.0.1
* Fix bug in Amazon SQS transport that would cause it to be unable to receive messages if the last created queue was not the transport's own input queue

## 2.0.0-a2
* Improve SQL transport expired messages cleanup to hit an index - thanks [xenoputtss]

## 2.0.0-a7
* Update to .NET 4.5.2 because it is the lowest framework version currently supported by Microsoft

## 2.0.0-a8
* Update NUnit dependency to 3.4.1

## 2.0.0-a9
* Fix file-based lock which was kept for longer than necessary (i.e. until GC would collect the `FileStream` that had not been properly disposed)

## 2.0.0-a10
* Experimentally multi-targeting .NET 4.5, 4.5.2, 4.6, and 4.6.1 (but it dit NOT work for 4.6 and 4.6.1)

## 2.0.0-a11
* Back to targeting .NET 4.5. only

## 2.0.0-a13
* Added ability to set up the advanced bus APIs for testing
* Proper assembly versions on DLLs

## 2.0.0-b01
* Move MSMQ things to separate repository and NuGet package `Rebus.Msmq`

## 2.0.0-b02
* Move SQL Server things to separate repository and NuGet package `Rebus.SqlServer`
* Remove legacy worker factory

## 2.0.0-b03
* Add separate wait methods to `ISyncBackoffStrategy` in order to differentiate between waiting because no more parallel operations are allowed, and waiting because no message was received
* Add `deliveryTimeoutSeconds` paramater to `Deliver` method of `SagaFixture` in order to allow for not timing out when e.g. step-debugging saga message processing

## 2.0.0
* This is Rebus 2 :)

## 2.0.1
* THIS is Rebus 2 :o)

## 2.0.2
* Allow for customizing how the ambient transaction context is accessed (e.g. by backing it with `HttpContext.Current.Items` if that works better in your case)

## 2.1.0
* Add ability to forward to multiple recipients with a transport message forwarder function

## 2.1.1
* Add missing Injectionist access methods on `RebusLoggingConfigurer`

## 2.1.2
* Fix bug that would only accept one set of correlation properties for each saga data type. Now it works when multiple saga handlers use the same saga data type.

## 2.1.3
* Add additional allowed saga correlation property types: `bool`, `byte`, `short`, and `long`

## 2.1.4
* Fix `BuiltinHandlerActivator` disposal race condition bug (could accidentally call a handler method with a `null` bus during shutdown)
* Fix bug that would cause a saga data instance explicitly added to the `SagaFixture` to be completely ignored - thanks [madstt]

## 2.1.5
* Add a couple of strategic logging statements around message dispatch

## 2.1.6
* Fix bug that can result in accidentally logging `IndexOutOfRangeExceptions`s when entering idle state, just when the server clock is adjusted back in time

## 3.0.0
* Add synchronous bus API which can be accessed via `bus.Advanced.SyncBus` that exposes synchronous `bus.Send`, etc. operations
* Add synchronous transaction context `DefaultSyncTransactionContext` which is completed with a synchronous `.Complete()` method
* Move Rebus 1 (i.e. versions <= 0.84.0) legacy compatibility feature to separate package: [Rebus.LegacyCompatibility](https://github.com/rebus-org/Rebus.LegacyCompatibility) (no worries: Rebus 2 and 3 can talk just fine!)
* Change transaction context API to be much more intuitive `DefaultTransactionContext` and `DefaultSyncTransactionContext` now automatically make the transaction context ambient
* Removed `GetCurrentClassLogger` because the stack frame walking turned out to be unreliable in the face of what seems to be new, more aggressive method inlining
* Add "out-of-handlers" data bus attachment read methods to `IDataBus`, which allows for reading attachments from outside message handlers

## 3.0.1
* Provide `FakeSyncBus` implementation of `ISyncBus`, which can be used to record events for testing

## 3.0.2
* Fix bug that caused uncompressed or unencrypted message bodies to be sent when forwarding messages

## 3.1.0
* Make transport message forwarder error behavior configurable, and make it default to moving the transport message to the error queue

## 3.1.1
* Fix bug that accidentally did not include the source queue address and the exception details on a fail-fast-forwarded transport message

## 3.1.2
* Fix regression that too eagerly validated a saga's correlation properties, leading to suddenly no longer being able to correlate by nested properties

## 3.1.3
* Make data storage test of last read time slightly more tolerant

## 3.1.4
* Fix `GetSimpleAssemblyQualifiedName` extension to handle generic message types better (i.e. make version tolerance work with type parameters too) - thanks [Rzpeg]

## 3.1.5
* Fix unspecified behavior that did not assign a revision number to a deleted saga
* Add contract test to verify correct behavior of saga persisters


## 4.0.0
* Hide some internal types that were never meant to be part of the API
* Add correlation configuration API to make it easy to correlate headers of incoming messages with saga data
* Change all logging to have named placeholders (Serilog style) instead of the semantically useless .NET placeholders (logging libraries that have no use for named placeholders can use `AbstractRebusLoggerFactory`'s `RenderString` method)
* Add experimental expression-based pipeline invoker (not enabled by default)
* Compile expression-based pipeline with [FastExpressionCompiler](https://github.com/dadhi/FastExpressionCompiler) (not enabled by default) - thanks [dadhi]
* Add .NET Core support (~~~targeting netstandard 1.6~~~) in addition to the current .NET 4.5 target - thanks [mvandevy]
* Move JSON serializer into core (still default, but now exposed so it can be customized)
* Rename transaction scope to `RebusTransactionScope` and make it expose `CompleteAsync()` and `Complete()` methods for completing in an async/sync manner respectively
* Add optional transport inspector service
* Extend `ILog` interface with warning signature that accepts an `Exception`
* Log uncaught exceptions from message handlers by passing real exceptions to the logger
* Change contract tests to better support "immutable containers" (i.e. containers that cannot have things registered in them after they are built) - thanks [trevorreeves]
* Set `rbs2-in-reply-to` header on replies containing message ID of the request
* Lower .NET Core compatible target to netstandard 1.3 - thanks [mirandaasm]
* Consolidate several header steps into one single `AssignDefaultHeadersStep` for outgoing messages
* Add routing slips
* Add `DeferLocal` and make deferral work analogous to sending
* Add ability to forcibly limit the length of the `rbs2-error-details` header to overcome limitations of the transport in cases where e.g. a long stack trace would make it impossible to forward a failed message to the error queue

## 4.0.1
* Fix exceptions thrown by default persistence implementations
* Fix exception thrown by Injectionist if a resolver cannot be found

## 4.1.0
* Add optional in-process locking middleware for saga instances
* Force initialization of the configuration system at startup to avoid risking that it is initialized in a Rebus handler (almost always triggered by Entity Framework, results in exceptions complaining that Rebus' transaction context contains unserializable things)
* Add option to externally configure the bus used internally by saga fixture, allowing for customizing everything but logging, transport, and saga storage (could e.g. be serialization) - thanks [boyanio]

## 4.2.0
* Decompression enabled by default because there's no reason why it shouldn't be
* Ability for default JSON serializer to deserialize based on the serialized type, enabling JSON objects devoid of .NET type information
* Fix it so that the file system transport does not output errors when in one-way client mode - thanks [pheiberg]
* Make Rebus buildable on OS X - thanks [bartul]
* Add ability to fail fast (i.e. immediately move the message to the error queue) either by throwing `FailFastException` or by configuring a custom `IFailFastChecker` to execute arbitrary logic - thanks [boyanio]

## 4.2.1
* Re-publish 4.2.0 because the Rebus.dll generated in 4.2.0 had version 1.0.0 and not the correct version

## 4.2.2
* Remove `.ConfigureAwait(false)` throughout, because it messes up the ability to carry ambient contexts into callbacks, which (via decorators) is one of Rebus' most important extensibility points - thanks [oliverhanappi]

## 4.3.0
* Add methods to in-mem persistence providers, making it easier to inspect them e.g. when testing - thanks [oliverhanappi]

## 5.0.0
* Move entire `Rebus.Testing` namespace to its own NuGet package: `Rebus.TestHelpers` (named "TestHelpers" to avoid confusing it with Rebus' own test project, and just because it's more appropriate)
* Ensure disposal of all `IDisposable` instances tracked by Injectionist in the event that an `IInitializable` fails during startup
* Add option to omit type information from serialized JSON text, thus saving lots of space and making the serialized JSON more pure at the expense of .NET object serialization power
* Provide `Defer` method on `IRoutingApi` allowing for easily deferring explicitly routed messages
* Ability to "name the bus", allowing for using something more descriptive than the default "Rebus 1", "Rebus 2"S names
* Change built-in console logging to use abbr. log lvls.
* Deferred start capability - call `Create()` instead of `Start()`, and the returned `IBusStarter` can be used to start message processing when it is desired to do so
* Make in-mem error tracking max age configurable (defaults to 10 minutes)
* Update JSON.NET dep to 11.0.1
* Introduce extensibility point `ITopicNameConvention` for defining how types are mapped to topics - thanks [heberop]
* Factor 40 improvement of "simple assembly-qualified type name" lookup
* Factor 2.5 improvement of type lookup in JSON serializer
* Introduce interface for locking saga data based on correlation properties, enabling the use of distributed locks - thanks [torangel]
* Fail fast when message cannot be delivered to any handlers, basically assuming that the situation will not correct itself anyway
* Add appropriate constructors to `IdempotencyData` to accomodate certain stubborn serializers
* Change signature of several methods of the backoff strategy to be async to avoid accidentally blocking thread pool threads - thanks [Plasma]
* Include symbol packages in the nuggie - thanks [poizan42]
* Add sender's address to all outgoing messages
* Ability to override `HandlerInvoker` instances created in the `ActivateHandlersStep` by overriding - thanks [Liero]
* Remove .NET Standard 1.3 as a target

## 5.0.1
* Throw exceptions in `TransactionContext` when hooking up callbacks after it has been completed (usually a sign of `async` stuff not being `await`ed) - thanks [mattwhetton]

## 5.0.2
* Add count methods form `InMemNetwork` - thanks [caspertdk]

## 5.1.0
* Port changes from 4.3.0 forward

## 5.2.0
* Allow for setting the default return address by going `.Options(o => o.SetDefaultReturnAddress("another-queue"))`

## 5.2.1
* Fix subtle cancellation/timing bug in `TplAsyncTask` which would lead to disposal taking several seconds, logging a warning that it took a long time

## 5.3.0
* Add cancellation support by adding a `CancellationToken` to the incoming step context. The token can be easily found by calling the `GetCancellationToken()` extension method on `IMessageContenxt`

## 5.3.1
* Fix bug in the "enforce exclusive saga access" feature that would result in an exception, if the same lock bucket got hit more than once

## 5.4.0
* Add `rbs2-defer-count` header, which is initially set to 1 when a message is deferred - each subsequent deferral of the message will then increment this counter

## 5.4.1
* Fix bug that would erroneously return a not-so-useful message context when inside a `RebusTransactionScope`

## 6.0.0
* Move data bus configuration to main configurer, so now you can go `.DataBus(d => d.StoreInFileSystem(@"\\network-path\folder"))`
* Add automatic claim check support, so you can have big messages transferred as data bus attachments by going e.g. `d.SendBigMessagesAsAttachments(bodySizeThresholdBytes: 200*1000)`
* Move cancellation support into the backoff strategy - thanks [nativenolde]
* Change log messages when changing number of workers to include the bus name - thanks [nativenolde]
* Add nifty fail fast helper on options configurer: `.Options(o => o.FailFastOn<TException>())` or `.Options(o => o.FailFastOn<TException>(when: ex => ex.Message.Contains("whatever")))`
* Change `RebusTransactionScope` so it can be used inside Rebus handlers (in case that would be handy to anyone)
* Change signature of all methods that accept headers to be `IDictionary<string, string>` - thanks [hellfirehd]
* Optimize Rebus' internal `TransactionContext` to use `event` (code-generated lists of callbacks) instead of `ConcurrentQueue<Func<Task>>` - thanks [jods4]
* Remove need for `ManualResetEvent` from Rebus' async background task implementation - thanks [jods4]
* Replace file system-based transport with a new one that uses temp-file+rename to send, and file-locking-w-FileShare-Delete to receive
* Add new data bus storage feature for querying by save time/last read time as well as deleting attachments
* Add `RebusTransactionScopeSuppressor`, which may be used in cases where an already active Rebus transaction scope must be temporarily suppressed
* Also target .NET Standard 2.1 (i.e. .NET Core 3.0)

## 6.0.1
* Make file system transport compatible with previous versions of Rebus - thanks [jr01]

## 6.1.0
* Introduce `IMessageTypeNameConvention`, which defaults to simple assembly-qualified type names. Can be customized by calling `.Serialization(s => s.UseCustomMessageTypeNames().Add(...))`

## 6.2.1
* Add extension method that makes it possible to manually dead-letter the message currently being handled

## 6.3.0
* Add ability to point out correlation properties of saga data by specifying them as strings - thanks [seankearon]

## 6.3.1
* Fix bug that would prevent fail-fast exceptions to cause messages to fail fast when thrown in a 2nd level retry

## 6.4.0
* Enforce handler registration in built-in handler activator before the bus is started. If one absolutely wants to add handlers AFTER having started the bus, it's still possible, simply by temporarily setting number of workers = 0

## 6.4.1
* Revert breaking change that snuck into the `IAsyncTaskFactory` interface

## 6.4.2
* Add [UsedImplicitly] annotation to [IHandleMessages] so implementations doesn't show up as unused - thanks [zlepper]

## 6.5.2
* Make the number of saga data conflict resolution attempts configurable
* Additional endpoint mapping methods - thanks [kendallb]

## 6.5.3
* `isCompliant: true` makes Rebus much more CLS compliant

## 6.5.4
* Use GUID in file name when doing read/write test in file system data bus storage to avoid colliding with other processes starting up - thanks [kendallb]

## 6.5.5
* Perform directory read/write access check in file system saga persister too - thanks [kendallb]

## 6.6.0
* Improve dispatch speed by using the nifty type-to-index trick
* Add serializer implementation for System.Text.Json, available via `.Serialization(s => s.UseSystemJson())` - thanks [mathiasnohall]

## 6.6.1
* Make it possible to set the `rbs2-senttime` header explicitly (i.e. it is no longer overwritten by the default headers step)

## 6.6.5
* Fix transaction context bug that would result in the completed flag being incorrectly set in situations where completion failed, which could lead to abort handlers not being called as they should

## 6.7.0
* Add ability for manual dead-lettering to carry a proper exception object, which will then be treated as every other exception caught when handling a poisonous message

## 7.0.0
* Change locking interface to be more generic - thanks [kendallb]
* Only target .NET Standard 2.0
* Change default JSON serializer to System.Text.Json (this is a BREAKING change! Please remember to `.Serializer(s => s.UseNewtonsoftJson()))` if you rely on e.g. inheritance and other advanced stuff
* Add `Configure.OneWayClient()` API which can be used to configure one-way clients, because handlers - and thus also the handler activator - are irrelevant - thanks [riezebosch]
* Fix typos in XML docs - thanks [torangel]
* Add extensibility point for fetching encryption keys from remote source, thus enabling centrally managed key rotation - thanks [torangel]
* Make `IErrorTracker` async to enable future distributed error tracking scenarios
* Add `ICorrelationErrorHandler` for handling cases where an incoming message matches a saga, but it cannot be correlated with an existing instance, and the message is not allowed to initiate a new saga. Enabled customization of how Rebus should behave.
* Extend allowed version ranges for Newtonsoft.Json to 12.0.1 <= v < 14 and System.Text.Json to 5 <= v < 8

## 7.1.0
* Add ability to enable encryption of data bus attachments

## 7.1.1
* Fix potential ambient transaction context issue that would inadvertently result in keeping a reference to the first transaction context if one or more long-running tasks were lazy-initialized as part of handling the first message

## 7.2.0
* Add `Events` configuration method on `OptionsConfigurer` to provide easier access to `BusLifetimeEvents`

## 8.0.1
* Change the sequence of events in the retry step, which paves the way for true, distributed error tracking (i.e. tracking delivery attempts across nodes)
* Refactor transaction context to make distinction between the ACK/NACK protocol and commit/rollback actions more clear
* Change error tracking to use `ExceptionInfo` instead of real `Exception`s to provide support for distributed error tracking
* Introduce `.Errors(e => ...)` configurer as a hook for configuring custom error trackers
* Add `IExceptionLogger` responsible for logging exceptions to enable consistent exception logging regardless of which error tracker is configured
* Prevent multiple bus instances from enlisting in the same transaction context, because it will result in undefined behavior
* Update accepted version range for System.Text.Json to include 8 and start with 6 (because 5 has dependency on lib reported as vulnerable)
* Update Newtonsoft.JSON accepted version to start at 13 because 12 is considered vulnerable

## 8.0.2
* Make `IsExternalInit` internal to avoid interfering with other tools that happen to be über sensitive
 
## 8.0.3
* Fix bug that prevented manually dead-lettering messages from a second-level retry handler

## 8.1.0
* Introduce IExceptionInfoFactory to enable keeping real `Exception` objects alongside exception details when tracking errors purely in-mem - thanks [ladenedge]

## 8.2.2
* Add optional `rbs2-delivery-count` header, which may be provided by transports that are capable of counting deliveries natively

## 8.2.4
* Fix behavior of fail-fast combined with 2nd level retries to be like before (i.e. failing fast will immediately trigger 2nd level retry)

## 8.3.0
* Alter how the message transaction context is managed when dead-lettering (i.e. the failing message context is now passed to `IErrorHandler` - it is now up to the error handler to create its own scope if it so desires) - thanks [AndreaCuneo]

## 8.4.0
* Add .NET 6 as compilation target to enable stripping dependencies that are included in the BCL - thanks [thompson-tomo]
 
## 8.4.1
* Fix bug that would cause the decryption step to try to decrypt un-encrypted contents when using encryption together with 2nd level retries

## 8.4.2
* Fix bug that would prevent using a default return address on a one-way client

## 8.4.3
* Fix bug in `EnforceExclusiveSagaAccessIncomingStepBase` that would cause lock buckets to be picked in an unstable way (in modern .NET, at least). Now using custom-implemented stable hash based on MD5

## 8.4.4
* Flatten hierarchy when looking up the dispatch method to better enable extension/alteration by inheritance - thanks [X509Whatever]

## 8.5.0
* Make a bunch of internal tweaks and improvements to improve performance across the board - thanks [Henr1k80]

## 8.6.0
* Another bunch of internal tweaks and improvements to improve performance across the board - thanks [Henr1k80]

## 8.6.1
* Fix regression that caused encrypted messages to end up unencrypted when dead-lettered

## 8.7.0
* Add additional compile targets and bump System.Text.Json dependency to at least 8.0.5 because almost all the previous versions have weaknesses

## 8.7.1
* Fix error during shutdown when saga locks were obtained - thanks [xhafan]


---

[AndreaCuneo]: https://github.com/AndreaCuneo
[arneeiri]: https://github.com/arneeiri
[bartul]: https://github.com/bartul
[bchavez]: https://github.com/bchavez
[bjomi]: https://github.com/bjomi
[boyanio]: https://github.com/boyanio
[caspertdk]: https://github.com/caspertdk
[dadhi]: https://github.com/dadhi
[dev4ce]: https://github.com/dev4ce
[dimajanzen]: https://github.com/dimajanzen
[DixonD-git]: https://github.com/DixonD-git
[dougkwilson]: https://github.com/dougkwilson
[fritsduus]: https://github.com/fritsduus
[gertjvr]: https://github.com/gertjvr
[hagbarddenstore]: https://github.com/hagbarddenstore
[Hangsolow]: https://github.com/Hangsolow
[heberop]: https://github.com/heberop
[hellfirehd]: https://github.com/hellfirehd
[Henr1k80]: https://github.com/Henr1k80
[jasperdk]: https://github.com/jasperdk
[jeffreyabecker]: https://github.com/jeffreyabecker
[jods4]: https://github.com/jods4
[joshua5822]: https://github.com/joshua5822
[jr01]: https://github.com/jr01
[jsvahn]: https://github.com/jsvahn
[kendallb]: https://github.com/kendallb
[kevbite]: https://github.com/kevbite
[krivin]: https://github.com/krivin
[ladenedge]: https://github.com/ladenedge
[Liero]: https://github.com/Liero
[madstt]: https://github.com/madstt
[maeserichar]: https://github.com/maeserichar
[mathiasnohall]: https://github.com/mathiasnohall
[mattwhetton]: https://github.com/mattwhetton
[maxx1337]: https://github.com/maxx1337
[mclausen]: https://github.com/mclausen
[meyce]: https://github.com/Meyce
[mirandaasm]: https://github.com/mirandaasm
[mgayeski]: https://github.com/mgayeski
[mgibas]: https://github.com/mgibas
[mortenherman]: https://github.com/mortenherman
[MrMDavidson]: https://github.com/MrMDavidson
[mvandevy]: https://github.com/mvandevy
[nativenolde]: https://github.com/nativenolde
[NKnusperer]: https://github.com/NKnusperer
[oguzhaneren]: https://github.com/oguzhaneren
[oliverhanappi]: https://github.com/oliverhanappi
[PeteProgrammer]: https://github.com/PeteProgrammer
[pheiberg]: https://github.com/pheiberg
[Plasma]: https://github.com/Plasma
[poizan42]: https://github.com/poizan42
[pruiz]: https://github.com/pruiz
[puzsol]: https://github.com/puzsol
[riezebosch]: https://github.com/riezebosch
[runes83]: https://github.com/runes83
[Rzpeg]: https://github.com/Rzpeg
[seankearon]: https://github.com/seankearon
[SvenVandenbrande]: https://github.com/SvenVandenbrande
[thompson-tomo]: https://github.com/thompson-tomo
[tiipe]: https://github.com/tiipe
[tobiaxor]: https://github.com/tobiaxor
[torangel]: https://github.com/torangel
[trevorreeves]: https://github.com/trevorreeves
[X509Whatever]: https://github.com/X509Whatever
[xenoputtss]: https://github.com/xenoputtss
[xhafan]: https://github.com/xhafan
[zabulus]: https://github.com/zabulus
[zlepper]: https://github.com/zlepper
