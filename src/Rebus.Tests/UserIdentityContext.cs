using System;
using System.Security.Principal;
using System.Threading;

namespace Rebus.Tests
{
    public class UserIdentityContext : IDisposable
    {
        readonly IPrincipal principalAtTheTimeOfEntering;

        public UserIdentityContext(string username)
        {
            principalAtTheTimeOfEntering = Thread.CurrentPrincipal;
            Thread.CurrentPrincipal = new GenericPrincipal(new GenericIdentity(username), new string[0]);
        }

        public void Dispose()
        {
            Thread.CurrentPrincipal = principalAtTheTimeOfEntering;
        }
    }
}