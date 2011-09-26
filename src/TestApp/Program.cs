using System;
using Rebus;
using Rebus.Tests;

namespace TestApp
{
    class Program : IHandlerBuilder, IProvideMessageTypes
    {
        static Bus server;
        static Bus client;

        static void Main()
        {
            try
            {
                client = new Bus(@".\private$\client", new Program(), new Program(), new MsmqQueue(new Program(), @".\private$\client"));
                server = new Bus(@".\private$\server", new Program(), new Program(), new MsmqQueue(new Program(), @".\private$\server"));

                client.Start();
                server.Start();

                var counter = 1;
                20.Times(() => client.Send(@".\private$\server", new HelloWorldRequest {Request = "Does it work? " + counter++}));
                client.Commit();

                Console.ReadKey();
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                client.Dispose();
                server.Dispose();
            }
        }

        public IHandleMessages<T> GetHandlerInstanceFor<T>()
        {
            var messageType = typeof(T);

            if (messageType == typeof(HelloWorldRequest))
            {
                return (IHandleMessages<T>)new HelloWorldRequestHandler { Bus = server };
            }

            if (messageType == typeof(HelloWorldResponse))
            {
                return (IHandleMessages<T>)new HelloWorldResponseHandler();
            }

            throw new InvalidOperationException(string.Format("Has no handler for {0}", messageType));
        }

        public void ReleaseHandlerInstance<T>(IHandleMessages<T> handler)
        {
        }

        class HelloWorldRequestHandler : IHandleMessages<HelloWorldRequest>
        {
            public Bus Bus { get; set; }

            public void Handle(HelloWorldRequest message)
            {
                Console.WriteLine("Got hello world request: {0}", message.Request);
            }
        }

        class HelloWorldResponseHandler : IHandleMessages<HelloWorldResponse>
        {
            public void Handle(HelloWorldResponse message)
            {
                Console.WriteLine("Got response: {0}", message.Response);
            }
        }

        public Type[] GetMessageTypes()
        {
            return new[] {typeof (HelloWorldRequest), typeof (HelloWorldResponse)};
        }
    }

    public class HelloWorldResponse
    {
        public string Response { get; set; }
    }

    public class HelloWorldRequest
    {
        public string Request { get; set; }
    }
}
