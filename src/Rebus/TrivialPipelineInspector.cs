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
    /// The trivial pipeline inspector is an implementation of <see cref="IInspectHandlerPipeline"/>
    /// that doesn't actually do anything. It can be used when you don't care about the handler
    /// pipeline, and then you can switch it for something else some time in the future when you
    /// feel like it.
    /// </summary>
    public class TrivialPipelineInspector : IInspectHandlerPipeline
    {
        /// <summary>
        /// Returns the unmodified sequence of handlers.
        /// </summary>
        public IEnumerable<IHandleMessages> Filter(object message, IEnumerable<IHandleMessages> handlers)
        {
            return handlers;
        }
    }
}