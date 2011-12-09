// Copyright 2011 Mogens Heller Grabe
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software distributed under the License is
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.
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

             var msmqMessageQueue = new MsmqMessageQueue(inputQueue);
             
             configurer.UseSender(msmqMessageQueue);
             configurer.UseReceiver(msmqMessageQueue);
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