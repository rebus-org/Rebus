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