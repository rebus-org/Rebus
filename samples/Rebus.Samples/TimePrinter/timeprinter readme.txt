The TimePrinter sample shows how a program can easily schedule recurring tasks via
messaging. This approach is cool because it benefits from Rebus' automatic retries
and error handling, dependency injection and component lifetime management, easy
monitoring etc.

The sample uses the builtin container adapter to create its handler, and an ordinary
System.DateTime as its only message type.

Just start the TimePrinter to run the sample.