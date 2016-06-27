using System.Security.Claims;

namespace Rebus.Recipes.Identity
{
    /// <summary>
    /// Serializes the claims principal because that needs special handling
    /// </summary>
    public interface IClaimsPrinicpalSerializer
    {
        /// <summary>
        /// Serializes the claims principal because that needs special handling
        /// </summary>
        string Serialize(ClaimsPrincipal userPrincipal);

        /// <summary>
        /// Deserializes the claims principal because that needs special handling.
        /// </summary>
        ClaimsPrincipal Deserialize(string value);
    }
}
