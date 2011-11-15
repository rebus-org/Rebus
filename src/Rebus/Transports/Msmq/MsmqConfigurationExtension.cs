using System;
using Rebus.Configuration.Configurers;

namespace Rebus.Transports.Msmq
{
    public static class MsmqConfigurationExtension
    {
         public static void UseMsmq(this TransportConfigurer configurer, string inputQueue)
         {
             if (inputQueue.Contains("@"))
             {
                 inputQueue = ParseQueueName(inputQueue);
             }
             else
             {
                 inputQueue = AssumeLocalQueue(inputQueue);
             }

             configurer.Use(new MsmqMessageQueue(inputQueue));
         }

        static string ParseQueueName(string inputQueue)
        {
            var tokens = inputQueue.Split('@');
            
            if (tokens.Length != 2)
            {
                throw new ArgumentException(string.Format("The specified MSMQ input queue is invalid!: {0}", inputQueue));
            }

            return string.Format(@"{0}\private$\{1}", tokens[0], tokens[1]);
        }

        static string AssumeLocalQueue(string inputQueue)
        {
            return string.Format(@".\private$\{0}", inputQueue);
        }
    }
}