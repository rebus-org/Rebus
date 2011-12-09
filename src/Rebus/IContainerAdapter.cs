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
    /// Extends a handler activator with the ability to register stuff.
    /// </summary>
    public interface IContainerAdapter : IActivateHandlers
    {
        /// <summary>
        /// Registers the given instance as an implementation of the specified service types.
        /// </summary>
        void RegisterInstance(object instance, params Type[] serviceTypes);
        
        /// <summary>
        /// Registers a mapping from the given service types to the specfied implementation type.
        /// If no service types are specified, the implementation is registered as an implementation
        /// of itself.
        /// </summary>
        void Register(Type implementationType, Lifestyle lifestyle, params Type[] serviceTypes);
        
        /// <summary>
        /// Should return whether the container contains something that claims to implement the specified
        /// service type.
        /// </summary>
        bool HasImplementationOf(Type serviceType);
        
        /// <summary>
        /// Gets an instance of the specified service type. Lifestyle is managed by the container.
        /// </summary>
        TService Resolve<TService>();

        /// <summary>
        /// Gets instances of all implementations of the given service type. Lifestyle is managed
        /// by the container.
        /// </summary>
        TService[] ResolveAll<TService>();
        
        /// <summary>
        /// Returns control of the given instance to the container.
        /// </summary>
        void Release(object obj);
    }
}