using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using Rebus.Bus;

namespace Rebus.Tests.Assumptions;

[TestFixture]
public class TestInternalsVisibleTo
{
    [TestCase("Rebus.TestHelpers")]
    [TestCase("Rebus.Tests")]
    [TestCase("Rebus.Tests.Contracts")]
    public void DoChek(string friendAssembly)
    {
        var match = typeof(IBus).Assembly.GetCustomAttributes(typeof(InternalsVisibleToAttribute), false)
            .OfType<InternalsVisibleToAttribute>()
            .FirstOrDefault(a => a.AssemblyName == friendAssembly);

        if (match != null) return;

        throw new AssertionException(
            $"Could not find [assembly: InternalsVisibleTo(...)] for '{friendAssembly}' in the Rebus DLL!");
    }
}