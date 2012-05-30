This sample shows how Rebus can be used with a request/reply pattern to make
communication with an external web service more robust.

The point is that the web service only succeeds in doing its job 1/3 of the
times it is called, and thus - if we dependended on it directly - it would
make our lives miserable.

Hiding it behind a wall of asynchronous reliable messaging with retries is
just infinitely useful in scanrios like this.