using System;
using FluentAssertions;
using NUnit.Framework;
using Rebus.Extensions;
using Rebus.Tests.Contracts;

namespace Rebus.Tests.Extensions
{
    [TestFixture]
    public class TestTypeExtensions : FixtureBase
    {
        [Test]
        public void SimplifiedNameForSimpleMessage()
        {
            // arrange
            var expectedType = typeof(SimpleMessage);
            var expectedTypeString = "Rebus.Tests.Extensions.SimpleMessage, Rebus.Tests";

            // act
            var actualTypeStringStatic = typeof(SimpleMessage).GetSimpleAssemblyQualifiedName();
            var actualTypeStringInstance = new SimpleMessage().GetType().GetSimpleAssemblyQualifiedName();
                
            var actualTypeStatic = Type.GetType(actualTypeStringStatic, false);
            var actualTypeInstance = Type.GetType(actualTypeStringInstance, false);


            // assert
            actualTypeStringStatic.ShouldBeEquivalentTo(expectedTypeString);
            actualTypeStringInstance.ShouldBeEquivalentTo(expectedTypeString);

            actualTypeStatic.ShouldBeEquivalentTo(expectedType);
            actualTypeInstance.ShouldBeEquivalentTo(expectedType);

        }

        [Test]
        public void SimplifiedNameForSimpleNestedMessage()
        {
            // arrange
            var expectedType = typeof(DeclaringClass.SimpleNestedMessage);
            var expectedTypeString = "Rebus.Tests.Extensions.DeclaringClass+SimpleNestedMessage, Rebus.Tests";

            // act
            var actualTypeStringStatic = typeof(DeclaringClass.SimpleNestedMessage).GetSimpleAssemblyQualifiedName();
            var actualTypeStringInstance = new DeclaringClass.SimpleNestedMessage().GetType().GetSimpleAssemblyQualifiedName();

            var actualTypeStatic = Type.GetType(actualTypeStringStatic, false);
            var actualTypeInstance = Type.GetType(actualTypeStringInstance, false);


            // assert
            actualTypeStringStatic.ShouldBeEquivalentTo(expectedTypeString);
            actualTypeStringInstance.ShouldBeEquivalentTo(expectedTypeString);

            actualTypeStatic.ShouldBeEquivalentTo(expectedType);
            actualTypeInstance.ShouldBeEquivalentTo(expectedType);
        }

        [Test]
        public void SimplifiedNameForSimpleGenericMessage()
        {
            // arrange
            var expectedType = typeof(SimpleGenericMessage<SimpleMessage>);
            var expectedTypeString = "Rebus.Tests.Extensions.SimpleGenericMessage`1[[Rebus.Tests.Extensions.SimpleMessage, Rebus.Tests]], Rebus.Tests";

            // act
            var actualTypeStringStatic = typeof(SimpleGenericMessage<SimpleMessage>).GetSimpleAssemblyQualifiedName();
            var actualTypeStringInstance = new SimpleGenericMessage<SimpleMessage>().GetType().GetSimpleAssemblyQualifiedName();
            var actualTypeStringInstanceRuntimeConstructed = typeof(SimpleGenericMessage<>).MakeGenericType(typeof(SimpleMessage)).GetSimpleAssemblyQualifiedName();

            var actualTypeStatic = Type.GetType(actualTypeStringStatic, false);
            var actualTypeInstance = Type.GetType(actualTypeStringInstance, false);
            var actualTypeInstanceRuntimeConstructed = Type.GetType(actualTypeStringInstanceRuntimeConstructed, false);
                
            // assert   
            actualTypeStringStatic.ShouldBeEquivalentTo(expectedTypeString);
            actualTypeStringInstance.ShouldBeEquivalentTo(expectedTypeString);
            actualTypeStringInstanceRuntimeConstructed.ShouldBeEquivalentTo(expectedTypeString);

            actualTypeStatic.ShouldBeEquivalentTo(expectedType);
            actualTypeInstance.ShouldBeEquivalentTo(expectedType);
            actualTypeInstanceRuntimeConstructed.ShouldBeEquivalentTo(expectedType);

        }

        [Test]
        public void SimplifiedNameForSimpleGenericNestedMessage()
        {
            // arrange
            var expectedType = typeof(DeclaringClass.SimpleNestedGenericMessage<SimpleMessage>);
            var expectedTypeString = "Rebus.Tests.Extensions.DeclaringClass+SimpleNestedGenericMessage`1[[Rebus.Tests.Extensions.SimpleMessage, Rebus.Tests]], Rebus.Tests";

            // act
            var actualTypeStringStatic = typeof(DeclaringClass.SimpleNestedGenericMessage<SimpleMessage>).GetSimpleAssemblyQualifiedName();
            var actualTypeStringInstance = new DeclaringClass.SimpleNestedGenericMessage<SimpleMessage>().GetType().GetSimpleAssemblyQualifiedName();
            var actualTypeStringInstanceRuntimeConstructed = typeof(DeclaringClass.SimpleNestedGenericMessage<>).MakeGenericType(typeof(SimpleMessage)).GetSimpleAssemblyQualifiedName();

            var actualTypeStatic = Type.GetType(actualTypeStringStatic, false);
            var actualTypeInstance = Type.GetType(actualTypeStringInstance, false);
            var actualTypeInstanceRuntimeConstructed = Type.GetType(actualTypeStringInstanceRuntimeConstructed, false);

            // assert   
            actualTypeStringStatic.ShouldBeEquivalentTo(expectedTypeString);
            actualTypeStringInstance.ShouldBeEquivalentTo(expectedTypeString);
            actualTypeStringInstanceRuntimeConstructed.ShouldBeEquivalentTo(expectedTypeString);

            actualTypeStatic.ShouldBeEquivalentTo(expectedType);
            actualTypeInstance.ShouldBeEquivalentTo(expectedType);
            actualTypeInstanceRuntimeConstructed.ShouldBeEquivalentTo(expectedType);
        }

        [Test]
        public void SimplifiedNameForComplexGenericMessage()
        {
            // arrange
            var expectedType = typeof(ComplexGenericMessage<SimpleMessage, int>);
            var expectedTypeString = "Rebus.Tests.Extensions.ComplexGenericMessage`2[[Rebus.Tests.Extensions.SimpleMessage, Rebus.Tests], [System.Int32, mscorlib]], Rebus.Tests";

            // act
            var actualTypeStringStatic = typeof(ComplexGenericMessage<SimpleMessage, int>).GetSimpleAssemblyQualifiedName();
            var actualTypeStringInstance = new ComplexGenericMessage<SimpleMessage, int>().GetType().GetSimpleAssemblyQualifiedName();
            var actualTypeStringInstanceRuntimeConstructed = typeof(ComplexGenericMessage<,>).MakeGenericType(typeof(SimpleMessage), typeof(int)).GetSimpleAssemblyQualifiedName();

            var actualTypeStatic = Type.GetType(actualTypeStringStatic, false);
            var actualTypeInstance = Type.GetType(actualTypeStringInstance, false);
            var actualTypeInstanceRuntimeConstructed = Type.GetType(actualTypeStringInstanceRuntimeConstructed, false);

            // assert   
            actualTypeStringStatic.ShouldBeEquivalentTo(expectedTypeString);
            actualTypeStringInstance.ShouldBeEquivalentTo(expectedTypeString);
            actualTypeStringInstanceRuntimeConstructed.ShouldBeEquivalentTo(expectedTypeString);

            actualTypeStatic.ShouldBeEquivalentTo(expectedType);
            actualTypeInstance.ShouldBeEquivalentTo(expectedType);
            actualTypeInstanceRuntimeConstructed.ShouldBeEquivalentTo(expectedType);

        }

        [Test]
        public void SimplifiedNameForComplexGenericNestedMessage()
        {
            // arrange
            var expectedType = typeof(DeclaringClass.ComplexNestedGenericMessage<SimpleMessage, int>);
            var expectedTypeString = "Rebus.Tests.Extensions.DeclaringClass+ComplexNestedGenericMessage`2[[Rebus.Tests.Extensions.SimpleMessage, Rebus.Tests], [System.Int32, mscorlib]], Rebus.Tests";

            // act
            var actualTypeStringStatic = typeof(DeclaringClass.ComplexNestedGenericMessage<SimpleMessage, int>).GetSimpleAssemblyQualifiedName();
            var actualTypeStringInstance = new DeclaringClass.ComplexNestedGenericMessage<SimpleMessage, int>().GetType().GetSimpleAssemblyQualifiedName();
            var actualTypeStringInstanceRuntimeConstructed = typeof(DeclaringClass.ComplexNestedGenericMessage<,>).MakeGenericType(typeof(SimpleMessage), typeof(int)).GetSimpleAssemblyQualifiedName();

            var actualTypeStatic = Type.GetType(actualTypeStringStatic, false);
            var actualTypeInstance = Type.GetType(actualTypeStringInstance, false);
            var actualTypeInstanceRuntimeConstructed = Type.GetType(actualTypeStringInstanceRuntimeConstructed, false);

            // assert   
            actualTypeStringStatic.ShouldBeEquivalentTo(expectedTypeString);
            actualTypeStringInstance.ShouldBeEquivalentTo(expectedTypeString);
            actualTypeStringInstanceRuntimeConstructed.ShouldBeEquivalentTo(expectedTypeString);

            actualTypeStatic.ShouldBeEquivalentTo(expectedType);
            actualTypeInstance.ShouldBeEquivalentTo(expectedType);
            actualTypeInstanceRuntimeConstructed.ShouldBeEquivalentTo(expectedType);
        }

        [Test]
        public void SimplifiedNameForSimpleNestedInGenericMessage()
        {
            // arrange
            var expectedType = typeof(DeclaringGenericClass<int>.SimpleNestedMessage);
            var expectedTypeString = "Rebus.Tests.Extensions.DeclaringGenericClass`1+SimpleNestedMessage[[System.Int32, mscorlib]], Rebus.Tests";

            // act
            var actualTypeStringStatic = typeof(DeclaringGenericClass<int>.SimpleNestedMessage).GetSimpleAssemblyQualifiedName();
            var actualTypeStringInstance = new DeclaringGenericClass<int>.SimpleNestedMessage().GetType().GetSimpleAssemblyQualifiedName();
            var actualTypeStringInstanceRuntimeConstructed = typeof(DeclaringGenericClass<>.SimpleNestedMessage).MakeGenericType(typeof(int)).GetSimpleAssemblyQualifiedName();

            var actualTypeStatic = Type.GetType(actualTypeStringStatic, false);
            var actualTypeInstance = Type.GetType(actualTypeStringInstance, false);
            var actualTypeInstanceRuntimeConstructed = Type.GetType(actualTypeStringInstanceRuntimeConstructed, false);

            // assert   
            actualTypeStringStatic.ShouldBeEquivalentTo(expectedTypeString);
            actualTypeStringInstance.ShouldBeEquivalentTo(expectedTypeString);
            actualTypeStringInstanceRuntimeConstructed.ShouldBeEquivalentTo(expectedTypeString);

            actualTypeStatic.ShouldBeEquivalentTo(expectedType);
            actualTypeInstance.ShouldBeEquivalentTo(expectedType);
            actualTypeInstanceRuntimeConstructed.ShouldBeEquivalentTo(expectedType);
        }

        [Test]
        public void SimplifiedNameForSimpleGenericNestedInGenericMessage()
        {
            // arrange
            var expectedType = typeof(DeclaringGenericClass<int>.SimpleNestedGenericMessage<SimpleMessage>);
            var expectedTypeString = "Rebus.Tests.Extensions.DeclaringGenericClass`1+SimpleNestedGenericMessage`1[[System.Int32, mscorlib], [Rebus.Tests.Extensions.SimpleMessage, Rebus.Tests]], Rebus.Tests";

            // act
            var actualTypeStringStatic = typeof(DeclaringGenericClass<int>.SimpleNestedGenericMessage<SimpleMessage>).GetSimpleAssemblyQualifiedName();
            var actualTypeStringInstance = new DeclaringGenericClass<int>.SimpleNestedGenericMessage<SimpleMessage>().GetType().GetSimpleAssemblyQualifiedName();
            var actualTypeStringInstanceRuntimeConstructed = typeof(DeclaringGenericClass<>.SimpleNestedGenericMessage<>).MakeGenericType(typeof(int), typeof(SimpleMessage)).GetSimpleAssemblyQualifiedName();

            var actualTypeStatic = Type.GetType(actualTypeStringStatic, false);
            var actualTypeInstance = Type.GetType(actualTypeStringInstance, false);
            var actualTypeInstanceRuntimeConstructed = Type.GetType(actualTypeStringInstanceRuntimeConstructed, false);

            // assert   
            actualTypeStringStatic.ShouldBeEquivalentTo(expectedTypeString);
            actualTypeStringInstance.ShouldBeEquivalentTo(expectedTypeString);
            actualTypeStringInstanceRuntimeConstructed.ShouldBeEquivalentTo(expectedTypeString);

            actualTypeStatic.ShouldBeEquivalentTo(expectedType);
            actualTypeInstance.ShouldBeEquivalentTo(expectedType);
            actualTypeInstanceRuntimeConstructed.ShouldBeEquivalentTo(expectedType);
        }

        [Test]
        public void SimplifiedNameForComplexGenericNestedInGenericMessage()
        {
            // arrange
            var expectedType = typeof(DeclaringGenericClass<int>.ComplexNestedGenericMessage<SimpleMessage, double>);
            var expectedTypeString = "Rebus.Tests.Extensions.DeclaringGenericClass`1+ComplexNestedGenericMessage`2[[System.Int32, mscorlib], [Rebus.Tests.Extensions.SimpleMessage, Rebus.Tests], [System.Double, mscorlib]], Rebus.Tests";

            // act
            var actualTypeStringStatic = typeof(DeclaringGenericClass<int>.ComplexNestedGenericMessage<SimpleMessage, double>).GetSimpleAssemblyQualifiedName();
            var actualTypeStringInstance = new DeclaringGenericClass<int>.ComplexNestedGenericMessage<SimpleMessage, double>().GetType().GetSimpleAssemblyQualifiedName();
            var actualTypeStringInstanceRuntimeConstructed = typeof(DeclaringGenericClass<>.ComplexNestedGenericMessage<,>).MakeGenericType(typeof(int), typeof(SimpleMessage), typeof(double)).GetSimpleAssemblyQualifiedName();

            var actualTypeStatic = Type.GetType(actualTypeStringStatic, false);
            var actualTypeInstance = Type.GetType(actualTypeStringInstance, false);
            var actualTypeInstanceRuntimeConstructed = Type.GetType(actualTypeStringInstanceRuntimeConstructed, false);

            // assert   
            actualTypeStringStatic.ShouldBeEquivalentTo(expectedTypeString);
            actualTypeStringInstance.ShouldBeEquivalentTo(expectedTypeString);
            actualTypeStringInstanceRuntimeConstructed.ShouldBeEquivalentTo(expectedTypeString);

            actualTypeStatic.ShouldBeEquivalentTo(expectedType);
            actualTypeInstance.ShouldBeEquivalentTo(expectedType);
            actualTypeInstanceRuntimeConstructed.ShouldBeEquivalentTo(expectedType);
        }
    }   


    public class SimpleGenericMessage<T1>
    {   
        public string Something { get; set; }
    }

    public class ComplexGenericMessage<T1, T2>
    {
        public string Something { get; set; }
    }

    public class SimpleMessage
    {
        public string Something { get; set; }

    }

    public class DeclaringClass
    {
        public class SimpleNestedMessage
        {
            public string SomethingElse { get; set; }
        }

        public class SimpleNestedGenericMessage<T1>
        {
            public string Something { get; set; }
        }

        public class ComplexNestedGenericMessage<T1, T2>
        {   
            public string Something { get; set; }
        }
    }

    public class DeclaringGenericClass<TDeclaring>
    {
        public class SimpleNestedMessage
        {
            public string SomethingElse { get; set; }
        }

        public class SimpleNestedGenericMessage<T1>
        {
            public string Something { get; set; }
        }

        public class ComplexNestedGenericMessage<T1, T2>
        {
            public string Something { get; set; }
        }
    }   
}   