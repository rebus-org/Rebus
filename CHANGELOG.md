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