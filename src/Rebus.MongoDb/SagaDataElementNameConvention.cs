using System;
using System.Reflection;
using MongoDB.Bson.Serialization.Conventions;

namespace Rebus.MongoDb
{
    public class SagaDataElementNameConvention : IElementNameConvention
    {
        readonly IElementNameConvention defaultElementNameConvention = new MemberNameElementNameConvention();
        
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