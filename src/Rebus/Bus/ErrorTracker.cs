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
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Rebus.Bus
{
    /// <summary>
    /// Class used by <see cref="RebusBus"/> to track errors between retries.
    /// </summary>
    public class ErrorTracker
    {
        readonly ConcurrentDictionary<string, TrackedMessage> trackedMessages = new ConcurrentDictionary<string, TrackedMessage>();

        public void Track(string id, Exception exception)
        {
            var trackedMessage = GetOrAdd(id);
            trackedMessage.AddError(exception);
        }

        public void Forget(string id)
        {
            TrackedMessage temp;
            trackedMessages.TryRemove(id, out temp);
        }

        public string GetErrorText(string id)
        {
            var trackedMessage = GetOrAdd(id);
            return trackedMessage.GetErrorMessages();
        }

        public bool MessageHasFailedMaximumNumberOfTimes(string id)
        {
            var trackedMessage = GetOrAdd(id);
            return trackedMessage.Errors >= 5;
        }

        TrackedMessage GetOrAdd(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException(string.Format("Id of message to track is null! Cannot track message errors with a null id"));
            }
            return trackedMessages.GetOrAdd(id, i => new TrackedMessage());
        }

        class TrackedMessage
        {
            readonly List<Exception> exceptions = new List<Exception>();

            public int Errors
            {
                get { return exceptions.Count; }
            }

            public void AddError(Exception exception)
            {
                exceptions.Add(exception);
            }

            public string GetErrorMessages()
            {
                return string.Join(Environment.NewLine, exceptions.Select(e => e.ToString()));
            }
        }
    }
}