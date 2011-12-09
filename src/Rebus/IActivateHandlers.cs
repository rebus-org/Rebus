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
using System.Collections.Generic;

namespace Rebus
{
    /// <summary>
    /// Implement this in order to delegate the instantiation work to your
    /// IoC container. Seriously, do it.
    /// </summary>
    public interface IActivateHandlers
    {
        /// <summary>
        /// Should get a sequence of handlers where each handler implements
        /// the <see cref="IHandleMessages{TMessage}"/> interface.
        /// </summary>
        IEnumerable<IHandleMessages<T>> GetHandlerInstancesFor<T>();
        
        /// <summary>
        /// Is called after each handler has been invoked. Please note that this method
        /// will be called for all handlers - i.e. if you add more handlers to the pipeline
        /// in the Filter method of <see cref="IInspectHandlerPipeline"/>, this method will
        /// be called for those additional handlers as well. This, in turn, allows you to
        /// implement <see cref="IInspectHandlerPipeline"/>, supplying your implementation
        /// of <see cref="IActivateHandlers"/> to that implementation, allowing any manually
        /// pulled handler instances to be released in the right way.
        /// </summary>
        void ReleaseHandlerInstances(IEnumerable<IHandleMessages> handlerInstances);
    }
}