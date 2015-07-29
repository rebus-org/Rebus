using System.Collections.Generic;
using NUnit.Framework;
using Rebus.Messages;

namespace Rebus.Jil.Tests
{
    [TestFixture]
    public class TestJilCapabilities
    {
        [Test]
        public void CanRoundtripClassWithConstructorParameters()
        {
            var msg = Roundtrip(new AwesomeImmutableMessage("yo!!", new RealValueType(23, "dings")));

            Assert.That(msg.Value, Is.EqualTo("yo!!"));
            Assert.That(msg.RealValueType.SomeValue, Is.EqualTo(23));
            Assert.That(msg.RealValueType.SomeUnit, Is.EqualTo("dings"));
        }

        class AwesomeImmutableMessage
        {
            AwesomeImmutableMessage()
            {
            }

            public AwesomeImmutableMessage(string value, RealValueType realValueType)
            {
                Value = value;
                RealValueType = realValueType;
            }

            public string Value { get; private set; }
            public RealValueType RealValueType { get; private set; }
        }

        class RealValueType
        {
            RealValueType()
            {
            }

            public RealValueType(int someValue, string someUnit)
            {
                SomeValue = someValue;
                SomeUnit = someUnit;
            }

            public int SomeValue { get; private set; }
            public string SomeUnit { get; private set; }
        }

        [Test]
        public void CanRoundtripSimpleClass()
        {
            var msg = Roundtrip(new SimpleClass
            {
                Decimal = 23.4M,
                Int = 4,
                String = "hej",
                SimpleEmbeddedObject = new SimpleEmbeddedObject
                {
                    Text = "hej igen"
                }
            });

            Assert.That(msg.Decimal, Is.EqualTo(23.4M));
            Assert.That(msg.Int, Is.EqualTo(4));
            Assert.That(msg.String, Is.EqualTo("hej"));
            Assert.That(msg.SimpleEmbeddedObject.Text, Is.EqualTo("hej igen"));
        }

        static TMessage Roundtrip<TMessage>(TMessage message) where TMessage : class
        {
            var serializer = new JilSerializer();
            var roundtrippedMessage =
                serializer.Deserialize(serializer.Serialize(new Message(new Dictionary<string, string>(), message)).Result)
                    .Result;

            return roundtrippedMessage.Body as TMessage;
        }

        class SimpleClass
        {
            public int Int { get; set; }
            public decimal Decimal { get; set; }
            public string String { get; set; }
            public SimpleEmbeddedObject SimpleEmbeddedObject { get; set; }
        }

        class SimpleEmbeddedObject
        {
            public string Text { get; set; }
        }
    }
}