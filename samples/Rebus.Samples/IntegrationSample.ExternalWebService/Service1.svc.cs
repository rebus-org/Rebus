using System;
using System.ServiceModel;

namespace IntegrationSample.ExternalWebService
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class Service1 : IService1
    {
        readonly Random random = new Random();

        readonly string[] greetings =
            new[]
                {
                    "good day, sir!",
                    "mjello!",
                    "hi!",
                    "yo!",
                    "zup?",
                    "hej",
                    "dav",
                    "good afternoon",
                    "good day",
                    "good night",
                    "guten abend",
                    "guten tag",
                    "yo dawg!",
                };

        public string GetGreeting()
        {
            if (random.Next(3) != 0)
            {
                throw new FaultException<string>("oh noes, something bad happened!");
            }

            return greetings[random.Next(greetings.Length)];
        }
    }
}
