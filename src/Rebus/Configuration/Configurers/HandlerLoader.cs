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
using System.Reflection;
using Rebus.Logging;

namespace Rebus.Configuration.Configurers
{
    public class HandlerLoader
    {
        static readonly ILog Log = RebusLoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        readonly IContainerAdapter containerAdapter;

        public HandlerLoader(IContainerAdapter containerAdapter)
        {
            this.containerAdapter = containerAdapter;
        }

        public HandlerLoader LoadFrom(Assembly assemblyToScan, params Assembly[] additionalAssemblies)
        {
            return LoadFrom(t => true, assemblyToScan, additionalAssemblies);
        }

        public HandlerLoader LoadFrom(Predicate<Type> shouldRegisterType, Assembly assemblyToScan, params Assembly[] additionalAssemblies)
        {
            Log.Debug("Loading handlers");

            var assembliesToScan = new[] { assemblyToScan }.Concat(additionalAssemblies);

            foreach(var assembly in assembliesToScan)
            {
                Log.Debug("Scanning {0}", assembly);

                RegisterHandlersFrom(assembly, shouldRegisterType);
            }

            return this;
        }

        void RegisterHandlersFrom(Assembly assembly, Predicate<Type> predicate)
        {
            var messageHandlers = assembly.GetTypes()
                .Select(t => new
                                 {
                                     Type = t,
                                     HandlerInterfaces = t.GetInterfaces().Where(IsHandler)
                                 })
                .Where(a => a.HandlerInterfaces.Any())
                .Where(a => predicate(a.Type))
                .SelectMany(a => a.HandlerInterfaces
                                     .Select(i => new
                                                      {
                                                          Service = i,
                                                          Implementation = a.Type,
                                                      }));

            foreach(var handler in messageHandlers)
            {
                Log.Debug("Registering handler {0} -> {1}", handler.Implementation, handler.Service);

                containerAdapter.Register(handler.Implementation, Lifestyle.Instance, handler.Service);
            }
        }

        static bool IsHandler(Type i)
        {
            return i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IHandleMessages<>);
        }
    }
}