using Rebus.Configuration.Configurers;

namespace Rebus.RabbitMQ
{
    public static class RabbitMqConfigurationExtensions
    {
         public static void UseRabbitMq(this TransportConfigurer configurer, string connectionString, string inputQueueName, string errorQueue)
         {
             var queue = new RabbitMqMessageQueue(connectionString, inputQueueName, errorQueue);
             configurer.UseSender(queue);
             configurer.UseReceiver(queue);
         }
    }
}