using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Tests.Contracts.Activation;

namespace Rebus.LightInject.Tests
{
    [TestFixture]
    public class LightInjectContainerTests : ContainerTests<LightInjectContainerAdapterFactory>
    {
    }
}
