using System.Collections.Generic;
using NUnit.Framework;
using Rebus.DataBus.InMem;

namespace Rebus.Tests.DataBus.InMem;

[TestFixture]
public class InMemDataStoreTests
{
    private InMemDataStore _inMemDataStore;

    [SetUp]
    public void SetUp()
    {
        _inMemDataStore = new InMemDataStore();
    }

    [Test]
    public void AttachmentIds_NoData_ReturnsEmpty()
    {
        Assert.That(_inMemDataStore.AttachmentIds, Is.Empty);
    }
        
    [Test]
    public void AttachmentIds_Data_ReturnsIDs()
    {
        _inMemDataStore.Save("test1", new byte[] {1, 2, 3});
        _inMemDataStore.Save("test2", new byte[] {4, 5, 6}, new Dictionary<string, string> {{"x", "y"}});

        Assert.That(_inMemDataStore.AttachmentIds, Is.EquivalentTo(new[] {"test1", "test2"}));
    }
        
    [Test]
    public void Contains_NoData_ReturnsFalse()
    {
        Assert.That(_inMemDataStore.Contains("test"), Is.False);
    }

    [Test]
    public void Contains_Data_ReturnsTrue()
    {
        _inMemDataStore.Save("test", new byte[] {1, 2, 3});
        Assert.That(_inMemDataStore.Contains("test"), Is.True);
    }

    [Test]
    public void Contains_DeletedData_ReturnsFalse()
    {
        _inMemDataStore.Save("test", new byte[] {1, 2, 3});

        _inMemDataStore.Delete("test");

        Assert.That(_inMemDataStore.Contains("test"), Is.False);
    }

    [Test]
    public void Contains_Null_ThrowsArgumentNullException()
    {
        Assert.That(() => _inMemDataStore.Contains(null), Throws.ArgumentNullException);
    }

    [Test]
    public void Delete_UnknownId_ReturnsFalse()
    {
        var result = _inMemDataStore.Delete("test");
        Assert.That(result, Is.False);
    }

    [Test]
    public void Delete_KnownId_DeletesData()
    {
        _inMemDataStore.Save("test", new byte[] {1, 2, 3}, new Dictionary<string, string> {{"x", "y"}});
        var result = _inMemDataStore.Delete("test");

        Assert.That(result, Is.True);
        Assert.That(_inMemDataStore.Contains("test"), Is.False);
    }

    [Test]
    public void Delete_Null_ThrowsArgumentNullException()
    {
        Assert.That(() => _inMemDataStore.Delete(null), Throws.ArgumentNullException);
    }
        
    [Test]
    public void Reset_Empty_DoesNothing()
    {
        Assert.That(() => _inMemDataStore.Reset(), Throws.Nothing);
        Assert.That(_inMemDataStore.SizeBytes, Is.Zero);
    }

    [Test]
    public void Reset_WithData_DeletesData()
    {
        _inMemDataStore.Save("test1", new byte[] {1, 2, 3});
        _inMemDataStore.Save("test2", new byte[] {4, 5, 6}, new Dictionary<string, string> {{"x", "y"}});

        Assert.That(_inMemDataStore.SizeBytes, Is.EqualTo(6));
        Assert.That(_inMemDataStore.LoadMetadata("test1"), Is.Empty);
        Assert.That(_inMemDataStore.LoadMetadata("test2")["x"], Is.EqualTo("y"));
            
        _inMemDataStore.Reset();

        Assert.That(_inMemDataStore.SizeBytes, Is.Zero);
        Assert.That(_inMemDataStore.AttachmentIds, Is.Empty);
        Assert.That(_inMemDataStore.Contains("test1"), Is.False);
        Assert.That(_inMemDataStore.Contains("test2"), Is.False);
        Assert.That(() => _inMemDataStore.Load("test1"), Throws.ArgumentException.With.Message.Contains("test1"));
        Assert.That(() => _inMemDataStore.Load("test2"), Throws.ArgumentException.With.Message.Contains("test2"));
        Assert.That(() => _inMemDataStore.LoadMetadata("test1"), Throws.ArgumentException.With.Message.Contains("test1"));
        Assert.That(() => _inMemDataStore.LoadMetadata("test2"), Throws.ArgumentException.With.Message.Contains("test2"));
    }
}