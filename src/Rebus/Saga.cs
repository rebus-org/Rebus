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
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Rebus
{
    public abstract class Saga
    {
        internal abstract ConcurrentDictionary<Type, Correlation> Correlations { get; set; }
        internal abstract bool Complete { get; set; }
        public abstract void ConfigureHowToFindSaga();
    }

    public class Saga<TData> : Saga where TData : ISagaData
    {
        public Saga()
        {
            Correlations = new ConcurrentDictionary<Type, Correlation>();
        }

        internal override ConcurrentDictionary<Type, Correlation> Correlations { get; set; }
        
        internal override bool Complete { get; set; }

        public override void ConfigureHowToFindSaga()
        {
        }

        protected Correlator<TData, TMessage> Incoming<TMessage>(Expression<Func<TMessage, object>> messageProperty)
        {
            return new Correlator<TData, TMessage>(messageProperty, this);
        }

        public TData Data { get; set; }

        protected void MarkAsComplete()
        {
            Complete = true;
        }
    }
}