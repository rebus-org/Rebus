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
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Tests
{
    /// <summary>
    /// Handler factory that allows lambdas to be registered as message handlers.
    /// </summary>
    public class HandlerActivatorForTesting : IActivateHandlers
    {
        readonly List<object> handlers = new List<object>();

        public class HandlerMethodWrapper<T> : IHandleMessages<T>
        {
            readonly Action<T> action;

            public HandlerMethodWrapper(Action<T> action)
            {
                this.action = action;
            }

            public void Handle(T message)
            {
                action(message);
            }
        }

        public HandlerActivatorForTesting Handle<T>(Action<T> handlerMethod)
        {
            return UseHandler(new HandlerMethodWrapper<T>(handlerMethod));
        }

        public HandlerActivatorForTesting UseHandler(object handler)
        {
            handlers.Add(handler);
            return this;
        }

        public IEnumerable<IHandleMessages<T>> GetHandlerInstancesFor<T>()
        {
            var handlerInstances = handlers
                .Where(h => h.GetType().GetInterfaces().Any(i => i == typeof(IHandleMessages<T>)))
                .Cast<IHandleMessages<T>>()
                .ToList();

            return handlerInstances;
        }

        public void ReleaseHandlerInstances(IEnumerable<IHandleMessages> handlerInstances)
        {
        }
    }
}