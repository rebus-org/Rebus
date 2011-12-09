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

namespace Rebus
{
    /// <summary>
    /// Abstract container adapter that bridges <see cref="IActivateHandlers"/> to
    /// <see cref="IContainerAdapter"/> methods, providing a base class off of which
    /// "real" container adapters can be easily made.
    /// </summary>
    public abstract class AbstractContainerAdapter : IContainerAdapter
    {
        public IEnumerable<IHandleMessages<T>> GetHandlerInstancesFor<T>()
        {
            return ResolveAll<IHandleMessages<T>>();
        }

        public void ReleaseHandlerInstances(IEnumerable<IHandleMessages> handlerInstances)
        {
            Release(handlerInstances);
        }

        public abstract void RegisterInstance(object instance, params Type[] serviceTypes);

        public abstract void Register(Type implementationType, Lifestyle lifestyle, params Type[] serviceTypes);
        
        public abstract bool HasImplementationOf(Type serviceType);

        public abstract T Resolve<T>();

        public abstract T[] ResolveAll<T>();

        public abstract void Release(object obj);
    }
}