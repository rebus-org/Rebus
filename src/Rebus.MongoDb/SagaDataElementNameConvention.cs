using System;
using System.Reflection;
using MongoDB.Bson.Serialization.Conventions;
using Ponder;

namespace Rebus.MongoDb
{
    class SagaDataElementNameConvention : IElementNameConvention
    {
        readonly IElementNameConvention defaultElementNameConvention = new MemberNameElementNameConvention();

        public SagaDataElementNameConvention()
        {
            RevisionMemberName = Reflect.Path<ISagaData>(d => d.Revision);
        }

        public string RevisionMemberName { get; private set; }

        public string GetElementName(MemberInfo member)
        {
            if (member == null)
            {
                throw new ArgumentNullException("member");
            }

            if (member.Name == RevisionMemberName)
            {
                return "_rev";
            }

            return defaultElementNameConvention.GetElementName(member);
        }
    }
}