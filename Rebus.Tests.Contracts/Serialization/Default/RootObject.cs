using System.Collections.Generic;

namespace Rebus.Tests.Contracts.Serialization.Default;

public class RootObject
{
    public List<BigObject> BigObjects { get; set; }
}