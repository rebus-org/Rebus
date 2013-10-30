using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using System.Web.Configuration;
using NUnit.Framework;
using Rebus.Bus;
using Rhino.Mocks;

namespace Rebus.Tests
{
    [TestFixture]
    public class TestAmbientTransactionContext : FixtureBase
    {
        IContext operationContext;
        IContext httpContext;
        Dictionary<string, object> items;
        object testObject;

        [SetUp]
        public void Setup()
        {
            httpContext = Mock<IContext>();
            operationContext = Mock<IContext>();
            items = new Dictionary<string, object>();

            testObject = new object();
        }

        [Test]
        public void ShouldSetObjectInHttpContextWhenPresent()
        {
            // Arrange
            httpContext.Stub(c => c.Items).Return(items);
            httpContext.Stub(c => c.InContext).Return(true);

            // Act
            using (new TransactionScope())
            {
                var ambientTransactionContext = new AmbientTransactionContext(httpContext, operationContext);
                ambientTransactionContext["testitem"] = testObject;
            }

            // Assert
            Assert.That(items["testitem"], Is.EqualTo(testObject));
        }

        [Test]
        public void ShouldGetObjectFromHttpContextWhenPresent()
        {
            // Arrange
            items["testitem"] = testObject;
            httpContext.Stub(c => c.Items).Return(items);
            httpContext.Stub(c => c.InContext).Return(true);

            // Act
            object actual;
            using (new TransactionScope())
            {
                var ambientTransactionContext = new AmbientTransactionContext(httpContext, operationContext);
                actual = ambientTransactionContext["testitem"];
            }

            // Assert
            Assert.That(actual, Is.EqualTo(testObject));
        }

        [Test]
        public void ShouldSetObjectInOperationContextWhenPresent()
        {
            // Arrange
            operationContext.Stub(c => c.Items).Return(items);
            operationContext.Stub(c => c.InContext).Return(true);

            // Act
            using (new TransactionScope())
            {
                var ambientTransactionContext = new AmbientTransactionContext(httpContext, operationContext);
                ambientTransactionContext["testitem"] = testObject;
            }

            // Assert
            Assert.That(items["testitem"], Is.EqualTo(testObject));
        }

        [Test]
        public void ShouldGetObjectFromOperationContextWhenPresent()
        {
            // Arrange
            items["testitem"] = testObject;
            operationContext.Stub(c => c.Items).Return(items);
            operationContext.Stub(c => c.InContext).Return(true);

            // Act
            object actual;
            using (new TransactionScope())
            {
                var ambientTransactionContext = new AmbientTransactionContext(httpContext, operationContext);
                actual = ambientTransactionContext["testitem"];
            }

            // Assert
            Assert.That(actual, Is.EqualTo(testObject));
        }

        [Test]
        public void ShouldSetObjectInAndGetObjectFromInternalContextWhenNoHostingContextPresent()
        {
            // Arrange

            // Act
            object actual;
            using (new TransactionScope())
            {
                var ambientTransactionContext = new AmbientTransactionContext(httpContext, operationContext);
                ambientTransactionContext["testitem"] = testObject;
                actual = ambientTransactionContext["testitem"];
            }

            // Assert
            Assert.That(actual, Is.EqualTo(testObject));
            httpContext.AssertWasNotCalled(c => c.Items);
            operationContext.AssertWasNotCalled(c => c.Items);
        }

        [Test]
        public void ShouldDisposeContexts()
        {
            // Arrange
            
            // Act
            using (new TransactionScope())
            {
                var ambientTransactionContext = new AmbientTransactionContext(httpContext, operationContext);
                ambientTransactionContext.Dispose();
            }

            // Assert
            httpContext.AssertWasCalled(c => c.Dispose());
            operationContext.AssertWasCalled(c => c.Dispose());
        }
    }
}

