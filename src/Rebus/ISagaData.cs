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
    public interface ISagaData
    {
        /// <summary>
        /// This is the ID of the saga. It should be set in the saga data, e.g. in the constructor
        /// of the class implementing this interface, ensuring that has been set when the saga
        /// is persisted the first time.
        /// </summary>
        Guid Id { get; set; }
        
        /// <summary>
        /// This is the revision of this saga. It may be used by the saga persister to implement
        /// optimistic locking. Not all saga persisters need to do this though.
        /// </summary>
        int Revision { get; set; }
    }
}