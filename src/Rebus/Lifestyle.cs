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
namespace Rebus
{
    /// <summary>
    /// Basic lifestyles that a given container implementation must support.
    /// </summary>
    public enum Lifestyle
    {
        /// <summary>
        /// The component is created the first time it is requested, subsequent requests must yield the same instance.
        /// </summary>
        Singleton,

        /// <summary>
        /// The component is created each time it is requested. It may, however - and that is up to you and the container
        /// of your choice - be scoped to something, like e.g. an aggregating implementation of <see cref="IHandleMessages{TMessage}"/>
        /// or something similar.
        /// </summary>
        Instance,
    }
}