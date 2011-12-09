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
using System.Linq.Expressions;
using Ponder;

namespace Rebus
{
    public class Correlator<TData, TMessage> : Correlation where TData : ISagaData
    {
        readonly Delegate messageProperty;
        readonly Saga<TData> saga;
        readonly string messagePropertyPath;
        string sagaDataPropertyPath;

        public Correlator(Expression<Func<TMessage, object>> messageProperty, Saga<TData> saga) 
        {
            this.messageProperty = messageProperty.Compile();
            this.saga = saga;
            messagePropertyPath = Reflect.Path(messageProperty);
        }

        internal override string SagaDataPropertyPath
        {
            get { return sagaDataPropertyPath; }
        }

        internal override string MessagePropertyPath
        {
            get { return messagePropertyPath; }
        }

        public override string FieldFromMessage<TMessage2>(TMessage2 message)
        {
            if (typeof(TMessage) != typeof(TMessage2))
            {
                throw new InvalidOperationException(
                    string.Format("Cannot extract {0} field from message of type {1} with func that takes a {2}",
                                  messagePropertyPath, typeof (TMessage2), typeof (TMessage)));
            }

            var property = (Func<TMessage2, object>)messageProperty;

            return (property(message) ?? "").ToString();
        }

        public void CorrelatesWith(Expression<Func<TData,object>> sagaDataProperty)
        {
            sagaDataPropertyPath = Reflect.Path(sagaDataProperty);

            saga.Correlations.TryAdd(typeof (TMessage), this);
        }
    }
}