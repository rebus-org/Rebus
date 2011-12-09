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
using System.Reflection;
using MongoDB.Bson.Serialization.Conventions;

namespace Rebus.MongoDb
{
    public class SagaDataElementNameConvention : IElementNameConvention
    {
        readonly CamelCaseElementNameConvention defaultElementNameConvention = new CamelCaseElementNameConvention();
        
        public string GetElementName(MemberInfo member)
        {
            if (member == null)
            {
                throw new ArgumentNullException("member");
            }

            if (member.Name == "Revision")
            {
                return "_rev";
            }

            return defaultElementNameConvention.GetElementName(member);
        }
    }
}