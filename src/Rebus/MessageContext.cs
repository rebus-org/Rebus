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

namespace Rebus
{
    /// <summary>
    /// Holds information about the message currently being handled on this particular thread.
    /// </summary>
    public class MessageContext : IDisposable
    {
        [ThreadStatic] static MessageContext current;

#if DEBUG
        public string StackTrace { get; set; }
#endif

        public static MessageContext Enter(string returnAddress)
        {
            if (current != null)
            {
#if DEBUG
                throw new InvalidOperationException(
                    string.Format(
                        @"Cannot establish new message context when one is already present!

Stacktrace of when the current message context was created:
{0}",
                        GetCurrent().StackTrace));
#else
                throw new InvalidOperationException(
                    string.Format("Cannot establish new message context when one is already present"));
#endif

            }
            current = new MessageContext
                          {
                              ReturnAddressOfCurrentTransportMessage = returnAddress
                          };

            return current;
        }

        MessageContext()
        {
            DispatchMessageToHandlers = true;

#if DEBUG
            StackTrace = Environment.StackTrace;
#endif
        }

        public string ReturnAddressOfCurrentTransportMessage { get; set; }

        public static MessageContext GetCurrent()
        {
            if (current == null)
            {
                throw new InvalidOperationException("No message context available - the MessageContext instance will"
                                                    + " only be set during the handling of messages, and it"
                                                    + " is available only on the worker thread.");
            }

            return current;
        }

        public static bool HasCurrent
        {
            get { return current != null; }
        }

        internal bool DispatchMessageToHandlers { get; set; }

        public void Dispose()
        {
            current = null;
        }
    }
}