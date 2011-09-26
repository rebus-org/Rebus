using System;
using Rebus;
using Rebus.Cruft;
using Sample.Server.Messages;

namespace Sample.Server
{
    class Program : IProvideMessageTypes, IHandlerFactory, IHandleMessages<Ping>
    {
        static void Main()
        {
            try
            {
                Run();
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static void Run()
        {
            var program = new Program();

            var msmqMessageQueue = new MsmqMessageQueue(@".\private$\sample.server", program);
            var bus = new RebusBus(program, msmqMessageQueue, msmqMessageQueue);

            program.Bus = bus;

            bus.Start();

            Console.WriteLine("Server listening...");
            Console.ReadKey();
        }

        public Type[] GetMessageTypes()
        {
            return typeof (Ping).Assembly.GetTypes();
        }

        public IHandleMessages<T> GetHandlerInstanceFor<T>()
        {
            try
            {
                return (IHandleMessages<T>) this;
            }
            catch(Exception e)
            {
                Console.WriteLine("Could not dispatch message of type {0}", typeof(T));
                return null;
            }
        }

        public void ReleaseHandlerInstance<T>(IHandleMessages<T> handlerInstance)
        {
            throw new NotImplementedException();
        }

        public void Handle(Ping message)
        {
            Console.WriteLine("Got ping: {0}", message.Message);

            Bus.Send(@".\private$\sample.client", new Pong { Message = string.Format("Server got '{0}'", message.Message) });
        }

        public RebusBus Bus { get; set; }
    }
}
