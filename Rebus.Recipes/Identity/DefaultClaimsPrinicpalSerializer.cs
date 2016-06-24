using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Newtonsoft.Json;

namespace Rebus.Recipes.Identity
{
    /// <summary>
    /// Default claims principal serializer
    /// </summary>
    public class DefaultClaimsPrinicpalSerializer : IClaimsPrinicpalSerializer
    {
        private class ClaimsIdentityLite
        {
            public string AuthenticationType { get; set; }
            public string NameType { get; set; }
            public string RoleType { get; set; }
            public List<ClaimLite> Claims { get; set; }
        }

        private class ClaimLite
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
        /// <param name="userPrincipal"></param>
        /// <returns></returns>
        public string Serialize(ClaimsPrincipal userPrincipal)
        {
            var tsr = userPrincipal.Identities.Select(i => new ClaimsIdentityLite
            {
                AuthenticationType = i.AuthenticationType,
                NameType = i.NameClaimType,
                RoleType = i.RoleClaimType,
                Claims = i.Claims.Select(c => new ClaimLite
                {
                    Issuer = c.Issuer,
                    OriginalIssuer = c.OriginalIssuer,
                    Type = c.Type,
                    Value = c.Value,
                    ValueType = c.ValueType
                }).ToList()
            }).ToList();
            return JsonConvert.SerializeObject(tsr, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }

        /// <summary>
        /// Deserializes the claims principal because that needs special handling.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public ClaimsPrincipal Deserialize(string value)
        {
            if (String.IsNullOrEmpty(value)) throw new InvalidOperationException("The serialized identity was invalid or corrupt.");
            var res = JsonConvert.DeserializeObject<List<ClaimsIdentityLite>>(value, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            var identities =
                res.Select(
                    i =>
                        new ClaimsIdentity(
                            i.Claims.Select(c => new Claim(c.Type, c.Value, c.ValueType, c.Issuer, c.OriginalIssuer)),
                            i.AuthenticationType, i.NameType, i.RoleType));
            return new ClaimsPrincipal(identities);
        }


    }
}