using NUnit.Framework;
using Rebus.Tests.Contracts.Serialization;

namespace Rebus.Tests.Serialization;

[TestFixture]
public class JsonSerializerTests : BasicSerializationTests<JsonSerializerFactory> { }