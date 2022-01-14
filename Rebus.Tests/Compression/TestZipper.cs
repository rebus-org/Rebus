using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using NUnit.Framework;
using Rebus.Compression;
using Rebus.Tests.Contracts;

namespace Rebus.Tests.Compression;

[TestFixture]
public class TestZipper : FixtureBase
{
    Zipper _zipper;
    const string Text = @"Hej med dig min ven, det her er en lang tekst med en del gentagelser.
Hej med dig min ven, det her er en lang tekst med en del gentagelser.
Hej med dig min ven, det her er en lang tekst med en del gentagelser.
Hej med dig min ven, det her er en lang tekst med en del gentagelser.";

    protected override void SetUp()
    {
        _zipper = new Zipper();
    }

    [Test]
    public void CanRoundtripBigBigString()
    {
        var bigString = string.Join("/", Enumerable.Range(0, 1000000));
        var bigStringBytes = Encoding.UTF8.GetBytes(bigString);
        var compressedBytes = _zipper.Zip(bigStringBytes);

        Console.WriteLine($"{bigStringBytes.Length/1024} kB => {compressedBytes.Length/1024} kB");

        var roundtrippedBytes = _zipper.Unzip(compressedBytes);
        var roundtrippedString = Encoding.UTF8.GetString(roundtrippedBytes);

        Assert.That(roundtrippedString, Is.EqualTo(bigString));
    }

    [Test]
    public void CanRoundtripSomeBytes()
    {
        var uncompressedBytes = Encoding.UTF8.GetBytes(Text);
        var compressedBytes = _zipper.Zip(uncompressedBytes);

        Assert.That(Encoding.UTF8.GetString(_zipper.Unzip(compressedBytes)), Is.EqualTo(Text));
        Assert.That(compressedBytes.Length, Is.LessThan(uncompressedBytes.Length));
    }

    [Test]
    public void WorksWithThisBadBoy()
    {
        var someId = Guid.NewGuid();

        var realisticObject = new ExecutePartialQueryRequest
        {
            //BlobNames = Enumerable.Range(0, 1000).Select(i => "blob" + i).ToArray(),
            Query = new QueryModel
            {
                QuerySteps =
                {
                    new QueryStep{EventType = "typeA", QueryStepOperator = QueryStepOperator.Intersect},
                    new QueryStep{EventType = "typeB", QueryStepOperator = QueryStepOperator.Except},
                }
            },
            SagaId = someId,
            WorkId = 23
        };

        var serializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        var realisticSerializationOfTheObject = JsonConvert.SerializeObject(realisticObject, serializerSettings);

        Console.WriteLine(realisticSerializationOfTheObject);

        var bytes = Encoding.UTF8.GetBytes(realisticSerializationOfTheObject);

        var roundtrippedBytes = _zipper.Unzip(_zipper.Zip(bytes));

        var objectString = Encoding.UTF8.GetString(roundtrippedBytes);

        var roundtrippedRealisticObject = (ExecutePartialQueryRequest)JsonConvert.DeserializeObject(objectString, serializerSettings);

        Assert.That(roundtrippedRealisticObject.SagaId, Is.EqualTo(someId));
    }

    // real model
    class ExecutePartialQueryRequest
    {
        public Guid SagaId { get; set; }
        public int WorkId { get; set; }

        public QueryModel Query { get; set; }
        public string[] BlobNames { get; set; }
    }

    enum QueryStepOperator { Intersect, Except }

    enum ComparisonOperator
    {
        GreaterThan,
        Equals,
        LessThan
    }

    class QueryModel
    {
        public QueryModel()
        {
            QuerySteps = new List<QueryStep>();
        }

        public List<QueryStep> QuerySteps { get; }
    }

    class QueryStep
    {
        /// <summary>
        /// If set, indicates which type of events this query step applies to
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// Determines whether this step is an inclusion/exclusion criteria
        /// </summary>
        public QueryStepOperator QueryStepOperator { get; set; }
    }

}