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
using Rebus.Configuration.Configurers;

namespace Rebus.Configuration
{
    /// <summary>
    /// Aids in configuring and adding a <see cref="RearrangeHandlersPipelineInspector"/>.
    /// </summary>
    public class FluentRearrangeHandlersPipelineInspectorBuilder
    {
        readonly RearrangeHandlersPipelineInspector rearranger = new RearrangeHandlersPipelineInspector();
        
        public FluentRearrangeHandlersPipelineInspectorBuilder(Type first, PipelineInspectorConfigurer configurer)
        {
            rearranger = new RearrangeHandlersPipelineInspector();
            rearranger.AddToOrder(first);
            configurer.Use(rearranger);
        }

        /// <summary>
        /// Configures the <see cref="RearrangeHandlersPipelineInspector"/> to re-arrange the handler
        /// pipeline, ensuring that the order specified by your calls to <see cref="RearrangeHandlersPipelineInspectorExtensions.First{THandler}"/>
        /// and <see cref="Then{TMessage}"/> are respected.
        /// </summary>
        public FluentRearrangeHandlersPipelineInspectorBuilder Then<TMessage>()
        {
            rearranger.AddToOrder(typeof (TMessage));
            return this;
        }
    }
}