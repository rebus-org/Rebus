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

## vNext

* 