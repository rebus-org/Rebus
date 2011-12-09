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
    public class OptimisticLockingException : ApplicationException
    {
        public OptimisticLockingException(ISagaData sagaData)
            : base(string.Format(@"Could not update saga of type {0} with _id {1} _rev {2} because someone else beat us to it",
            sagaData.GetType(), sagaData.Id, sagaData.Revision))
        {
        }
    }
}