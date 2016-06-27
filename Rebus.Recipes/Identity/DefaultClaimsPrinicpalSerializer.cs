using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Rebus.Serialization;

namespace Rebus.Recipes.Identity
{
    /// <summary>
    /// Default claims principal serializer
    /// </summary>
    public class DefaultClaimsPrinicpalSerializer : IClaimsPrinicpalSerializer
    {
        readonly GenericJsonSerializer _jsonSerializer = new GenericJsonSerializer();

        class ClaimsIdentityLite
        {
            public string AuthenticationType { get; set; }
            public string NameType { get; set; }
            public string RoleType { get; set; }
            public List<ClaimLite> Claims { get; set; }
        }

        class ClaimLite
        {
            public string Type { get; set; }
            public string Value { get; set; }
            public string ValueType { get; set; }
            public string Issuer { get; set; }
            public string OriginalIssuer { get; set; }
        }

        /// <summary>
        /// Serializes the claims principal because that needs special handling
        /// </summary>
        public string Serialize(ClaimsPrincipal userPrincipal)
        {
            var identities = userPrincipal.Identities
                .Select(i => new ClaimsIdentityLite
                {
                    AuthenticationType = i.AuthenticationType,
                    NameType = i.NameClaimType,
                    RoleType = i.RoleClaimType,
                    Claims = i.Claims
                        .Select(c => new ClaimLite
                        {
                            Issuer = c.Issuer,
                            OriginalIssuer = c.OriginalIssuer,
                            Type = c.Type,
                            Value = c.Value,
                            ValueType = c.ValueType
                        })
                        .ToList()
                })
                .ToList();

            return _jsonSerializer.Serialize(identities);
        }

        /// <summary>
        /// Deserializes the claims principal because that needs special handling.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public ClaimsPrincipal Deserialize(string value)
        {
            if (string.IsNullOrEmpty(value)) throw new InvalidOperationException("The serialized identity was invalid or corrupt.");

            var list = _jsonSerializer.Deserialize<List<ClaimsIdentityLite>>(value);

            var identities = list
                .Select(i => new ClaimsIdentity(i.Claims
                    .Select(c => new Claim(c.Type, c.Value, c.ValueType, c.Issuer, c.OriginalIssuer)),
                    i.AuthenticationType, i.NameType, i.RoleType));

            return new ClaimsPrincipal(identities);
        }
    }
}