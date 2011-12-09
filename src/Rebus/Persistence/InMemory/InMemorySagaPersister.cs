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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Ponder;

namespace Rebus.Persistence.InMemory
{
    public class InMemorySagaPersister : IStoreSagaData, IEnumerable<ISagaData>
    {
        readonly ConcurrentDictionary<Guid, ISagaData> data = new ConcurrentDictionary<Guid, ISagaData>();

        public virtual void Save(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
        {
            var key = sagaData.Id;
            data[key] = sagaData;
        }

        public void Delete(ISagaData sagaData)
        {
            ISagaData temp;
            data.TryRemove(sagaData.Id, out temp);
        }

        public virtual ISagaData Find(string sagaDataPropertyPath, object fieldFromMessage, Type sagaDataType)
        {
            foreach (var sagaData in data)
            {
                var valueFromSagaData = (Reflect.Value(sagaData.Value, sagaDataPropertyPath) ?? "").ToString();

                if (valueFromSagaData.Equals((fieldFromMessage ?? "").ToString()))
                {
                    return sagaData.Value;
                }
            }
            return null;
        }

        public void UseIndex(string[] sagaDataPathsToIndex)
        {
            
        }

        public IEnumerator<ISagaData> GetEnumerator()
        {
            return data.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}