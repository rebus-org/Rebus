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

* Fixed constantly generated warning in timeout manager - thx [hagbarddenstore](https://github.com/hagbarddenstore)

## 0.39.0

* Added ability to compress message bodies as well.

## 0.40.0

* Timeout manager SQL persistence oddity fixed: Explicit bigint PK instead of compound thing that could potentially lead to missed timeouts - thx [krivin](https://github.com/krivin)
* `IStartableBus` API extended with the ability to specify number of workers - thx [krivin](https://github.com/krivin)

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

* Use hybrid stash model for transaction context, allowing it to overcome thread discontinuity in ASP.NET and WCF - thanks [jasperdk](https://github.com/jasperdk)
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
* Dispose MSMQ messages after use - thanks [dev4ce](https://github.com/dev4ce)

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

## 054.2

* `XmlSubscriptionStorage` automatically creates directory pointed to by the subscription XML file path - thanks [hagbarddenstore](https://github.com/hagbarddenstore)
