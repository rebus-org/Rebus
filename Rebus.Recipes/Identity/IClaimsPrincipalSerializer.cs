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
        /// <param name="userPrincipal"></param>
        /// <returns></returns>
        string Serialize(ClaimsPrincipal userPrincipal);
        /// <summary>
        /// Deserializes the claims principal because that needs special handling.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        ClaimsPrincipal Deserialize(string value);
    }
}
